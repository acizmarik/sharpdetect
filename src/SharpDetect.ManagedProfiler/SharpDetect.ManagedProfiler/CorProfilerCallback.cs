using SharpDetect.Common.Messages;
using SharpDetect.Profiler.Communication;
using SharpDetect.Profiler.Hooks;
using SharpDetect.Profiler.Logging;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpDetect.Profiler;

internal unsafe class CorProfilerCallback : ICorProfilerCallback2
{
    public NativeObjects.ICorProfilerCallback2 Object { get; private set; }
    public static CorProfilerCallback Instance { get; private set; } = null!;

    private readonly ConcurrentDictionary<ModuleId, Module> moduleLookup;
    private readonly ConcurrentDictionary<AssemblyId, Assembly> assemblyLookup;
    private readonly ConcurrentDictionary<FunctionId, Method> methodHooks;
    private readonly AsmUtilities asmUtilities;
    private MessagingClient messagingClient;
    private MessageFactory messageFactory;
    private ICorProfilerInfo3 corProfilerInfo;
    private InstrumentationContext? instrumentationContext;
    private ModuleId? coreLibraryModuleId;
    
    public CorProfilerCallback()
    {
        AsyncIO.ForceDotNet.Force();
        Object = NativeObjects.ICorProfilerCallback2.Wrap(this);
        Logger.Initialize(
            LogLevel.Debug,
            new ConsoleSink(),
            new FileSink("profiler-log.txt", append: false));

        moduleLookup = new();
        assemblyLookup = new();
        methodHooks = new();
        messagingClient = null!;
        messageFactory = null!;
        corProfilerInfo = null!;
        asmUtilities = new();
        Instance = this;
    }

    public HResult Initialize(nint pICorProfilerInfoUnk)
    {
        // Obtain ICorProfilerInfo
        var iunknown = NativeObjects.IUnknown.Wrap(pICorProfilerInfoUnk);
        if (iunknown.QueryInterface(in KnownGuids.ICorProfilerInfo3, out var ptr) != HResult.S_OK)
        {
            Logger.LogError($"Could not retrieve {nameof(KnownGuids.ICorProfilerInfo3)}");
            return HResult.E_FAIL;
        }

        corProfilerInfo = NativeObjects.ICorProfilerInfo3.Wrap(ptr);
        messageFactory = new MessageFactory(corProfilerInfo);
        messagingClient = new MessagingClient(messageFactory);
        messagingClient.Start();

        corProfilerInfo.GetRuntimeInformation(out _, out var runtimeType, out var majorVer, out var minorVer, out var buildVer, out var qfVer, 0, out _, null);
        Logger.LogInformation($"Architecture: {IntPtr.Size * 8}bit");
        Logger.LogInformation($"RuntimeType: {runtimeType}");
        Logger.LogInformation($"RuntimeVersion: v{majorVer}.{minorVer}.{buildVer}.{qfVer}");

        // Initialize runtime profiling capabilities
        if (!corProfilerInfo.SetEventMask(
            COR_PRF_MONITOR.COR_PRF_MONITOR_MODULE_LOADS |
            COR_PRF_MONITOR.COR_PRF_MONITOR_CLASS_LOADS |
            COR_PRF_MONITOR.COR_PRF_MONITOR_THREADS |
            COR_PRF_MONITOR.COR_PRF_MONITOR_JIT_COMPILATION |
            COR_PRF_MONITOR.COR_PRF_MONITOR_ENTERLEAVE |
            COR_PRF_MONITOR.COR_PRF_ENABLE_FRAME_INFO |
            COR_PRF_MONITOR.COR_PRF_ENABLE_FUNCTION_ARGS |
            COR_PRF_MONITOR.COR_PRF_ENABLE_FUNCTION_RETVAL))
        {
            Logger.LogError($"Could not set profiling event mask");
            return HResult.E_FAIL;
        }

        // Register method enter/leave hooks
        if (!MethodHooks.Register(corProfilerInfo, asmUtilities))
        {
            Logger.LogError($"Could not register method enter/leave hooks");
            return HResult.E_FAIL;
        }

        messagingClient.SendNotification(messageFactory.CreateProfilerInitializedNotification());
        Logger.LogInformation($"Profiler initialized");
        return HResult.S_OK;
    }

    public HResult Shutdown()
    {
        messagingClient.SendNotification(messageFactory.CreateProfilerDestroyedNotification());
        Logger.LogInformation("Profiler shutting down");
        Logger.Terminate();
        asmUtilities.Dispose();
        return HResult.S_OK;
    }

    public HResult AppDomainCreationStarted(AppDomainId appDomainId)
    {
        return HResult.S_OK;
    }

    public HResult AppDomainCreationFinished(AppDomainId appDomainId, HResult hrStatus)
    {
        return HResult.S_OK;
    }

    public HResult AppDomainShutdownStarted(AppDomainId appDomainId)
    {
        return HResult.S_OK;
    }

    public HResult AppDomainShutdownFinished(AppDomainId appDomainId, HResult hrStatus)
    {
        return HResult.S_OK;
    }

    public HResult AssemblyLoadStarted(AssemblyId assemblyId)
    {
        return HResult.S_OK;
    }

    public HResult AssemblyLoadFinished(AssemblyId assemblyId, HResult hrStatus)
    {
        return HResult.S_OK;
    }

    public HResult AssemblyUnloadStarted(AssemblyId assemblyId)
    {
        return HResult.S_OK;
    }

    public HResult AssemblyUnloadFinished(AssemblyId assemblyId, HResult hrStatus)
    {
        return HResult.S_OK;
    }

    public HResult ModuleLoadStarted(ModuleId moduleId)
    {
        return HResult.S_OK;
    }

    public HResult ModuleLoadFinished(ModuleId moduleId, HResult hrStatus)
    {
        // We are not interested in unsuccessful assembly loads
        if (hrStatus != HResult.S_OK)
            return HResult.S_OK;
        
        Module module;
        Assembly assembly;
        try
        {
            // Create and initialize metadata wrappers
            module = new Module(moduleId, corProfilerInfo);
            assembly = new Assembly(moduleId, corProfilerInfo);
            moduleLookup.AddOrUpdate(moduleId, module, (_, _) => module);
            assemblyLookup.AddOrUpdate(assembly.AssemblyId, assembly, (_, _) => assembly);
            Logger.LogDebug($"Loaded module {module.Name} from {module.FullPath}");
            var newNotificationId = messagingClient.GetNewNotificationId();
            var requestFuture = messagingClient.ReceiveRequest(newNotificationId);
            messagingClient.SendNotification(messageFactory.CreateModuleLoadedNotification(moduleId, module.FullPath), newNotificationId);

            // Check what kind of module this is (core module gets loaded always first)
            Func<Module, Assembly, HResult> handler = (!coreLibraryModuleId.HasValue) ? HandleCoreModuleLoaded : HandleRegularModuleLoaded;
            var result = handler(module, assembly);

            // Wait for request (what to do with this module)
            var request = requestFuture.Result;
            if (result)
            {
                // Check if we need to wrap extern methods
                result = (request.Wrapping is Request_Wrapping wrappingRequest) ?
                    HandleExternMethodsWrapping(module, instrumentationContext!, wrappingRequest) :
                    Instrumentation.ImportWrapperMethods(instrumentationContext!, assembly, module);
            }

            // Respond to module alternations request
            messagingClient.SendResponse(messageFactory.CreateResponse(request, result == HResult.S_OK));
            return HResult.S_OK;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Could not construct metadata wrapper for module and/or assembly due to: {ex}");
            return HResult.E_FAIL;
        }
    }

    private HResult HandleCoreModuleLoaded(Module module, Assembly assembly)
    {
        // We are loading core library
        coreLibraryModuleId = module.Id;

        // Emit helper type
        var context = new InstrumentationContext(corProfilerInfo, assembly, module);
        if (!Instrumentation.InjectEventDispatcherType(ref context))
        {
            Logger.LogError($"Could not emit event dispatcher type into {module.Name}");
            return HResult.E_FAIL;
        }

        // Emit helper methods
        if (!Instrumentation.InjectHelperMethods(context))
        {
            Logger.LogError($"Could not emit one or multiple methods into type \"{Instrumentation.DispatcherTypeName}\"");
            return HResult.E_FAIL;
        }

        instrumentationContext = context;
        return HResult.S_OK;
    }

    private HResult HandleRegularModuleLoaded(Module module, Assembly assembly)
    {
        // We are loading a regular assembly => add reference to core library
        var coreModule = moduleLookup[coreLibraryModuleId!.Value];
        var coreAssembly = assemblyLookup[coreModule.AssemblyId];
        if (!coreAssembly.GetAssemblyProps(out var name, out var publicKey, out var cbPublicKey, out var metadata, out var flags))
        {
            Logger.LogError($"Could not obtain assembly props of core assembly {coreModule.Name}");
            return HResult.E_FAIL;
        }
        if (!assembly.AddOrGetAssemblyRef($"{name}\0", publicKey, cbPublicKey, metadata, flags, out var assemblyRef))
        {
            Logger.LogError($"Could not add assembly reference for module {coreModule.Name} into {module.Name}");
            return HResult.E_FAIL;
        }
        Logger.LogDebug($"Imported assembly reference for {coreModule.Name} ({assemblyRef.Value}) into {module.Name}");

        // Import event dispatcher type
        if (!module.AddTypeRef(assemblyRef, Instrumentation.DispatcherTypeName, out var typeRef))
        {
            Logger.LogError($"Could not add type reference for \"{Instrumentation.DispatcherTypeName}\" into {module.Name}");
            return HResult.E_FAIL;
        }
        Logger.LogDebug($"Imported type reference for \"{Instrumentation.DispatcherTypeName}\" ({typeRef.Value}) into {module.Name}");

        // Import all helper methods on dispatcher type
        if (!Instrumentation.ImportHelperMethods(instrumentationContext!, module, typeRef))
        {
            Logger.LogError($"Could not import one or multiple methods from type \"{Instrumentation.DispatcherTypeName}\"");
            return HResult.E_FAIL;
        }

        return HResult.S_OK;
    }

    private HResult HandleExternMethodsWrapping(Module module, InstrumentationContext context, Request_Wrapping wrappingRequest)
    {
        foreach (var externInfo in wrappingRequest.MethodsToWrap)
        {
            var typeDef = new MdTypeDef((int)externInfo.TypeToken);
            var methodDef = new MdMethodDef((int)externInfo.FunctionToken);

            if (!Instrumentation.InjectWrapperMethod(
                context,
                module,
                typeDef,
                methodDef,
                (ushort)externInfo.ParametersCount,
                out var _))
            {
                Logger.LogError($"Could not wrap one or multiple requested methods for module {module.FullPath}");
                return HResult.E_FAIL;
            }
        }

        return HResult.S_OK;
    }

    public HResult ModuleUnloadStarted(ModuleId moduleId)
    {
        return HResult.S_OK;
    }

    public HResult ModuleUnloadFinished(ModuleId moduleId, HResult hrStatus)
    {
        return HResult.S_OK;
    }

    public HResult ModuleAttachedToAssembly(ModuleId moduleId, AssemblyId assemblyId)
    {
        return HResult.S_OK;
    }

    public HResult ClassLoadStarted(ClassId classId)
    {
        return HResult.S_OK;
    }

    public HResult ClassLoadFinished(ClassId classId, HResult hrStatus)
    {
        // We are not interested in unsuccessful class loads
        if (hrStatus != HResult.S_OK)
            return HResult.S_OK;

        // Get defining module and class token
        if (!corProfilerInfo.GetClassIDInfo2(classId, out var moduleId, out var typeDef, out _, 0, out _, out _))
        {
            Logger.LogError($"Could not load information about loaded type with classId {classId}");
            return HResult.E_FAIL;
        }

        // Pack information about class load
        var message = messageFactory.CreateTypeLoadedNotification(moduleId, typeDef);
        messagingClient.SendNotification(message);

        return HResult.S_OK;
    }

    public HResult ClassUnloadStarted(ClassId classId)
    {
        return HResult.S_OK;
    }

    public HResult ClassUnloadFinished(ClassId classId, HResult hrStatus)
    {
        return HResult.S_OK;
    }

    public HResult FunctionUnloadStarted(FunctionId functionId)
    {
        return HResult.S_OK;
    }

    public HResult JITCompilationStarted(FunctionId functionId, bool fIsSafeToBlock)
    {
        // Obtain information about the method
        if (!corProfilerInfo.GetFunctionInfo2(functionId, default, out _, out var moduleId, out var token, 0, out _, null) ||
            !moduleLookup[moduleId].GetMethodProps(new MdMethodDef(token.Value), out var typeDef, out var name, out _, out _))
        {
            Logger.LogError($"Could not load information about method with functionId {functionId}");
            return HResult.E_FAIL;
        }

        // Make sure the client registers that we are waiting for a request
        var message = messageFactory.CreateJITCompilationStartedNotification(moduleId, typeDef, new MdMethodDef(token.Value));
        var newNotificationId = messagingClient.GetNewNotificationId();
        var requestFuture = messagingClient.ReceiveRequest(newNotificationId);
        messagingClient.SendNotification(message, newNotificationId);

        // Wait for request
        var result = HResult.S_OK;
        var request = requestFuture.Result;
        if (request.Instrumentation is Request_Instrumentation instrumentationRequest)
        {
            var module = moduleLookup[moduleId];
            var methodDef = new MdMethodDef(token.Value);
            result = HandleInstrumentationRequest(instrumentationRequest, module, functionId, name!, typeDef, methodDef);
        }

        // Respond
        var response = messageFactory.CreateResponse(request, result == HResult.S_OK);
        messagingClient.SendNotification(response);

        return HResult.S_OK;
    }

    private HResult HandleInstrumentationRequest(
        Request_Instrumentation request, 
        Module module, 
        FunctionId functionId, 
        string methodName, 
        MdTypeDef typeDef, 
        MdMethodDef methodDef)
    {
        if (request.InjectHooks)
        {
            // Prepare information about captured arguments, if available
            var totalArgumentsSize = 0UL;
            var totalIndirectArgumentsSize = 0UL;
            var argumentInfos = new List<(ushort, ushort, bool)>();
            if (request.ArgumentInfos.Span.Length > 0)
            {
                var data = request.ArgumentInfos;
                var indirects = request.PassingByRefInfos;
                for (var i = 0; i < data.Length; i += 4)
                {
                    ushort index = (ushort)(data[i + 1] << 8 | data[i]);
                    ushort size = (ushort)(data[i + 3] << 8 | data[i + 2]);
                    bool isIndirect = (indirects[index / 8] & (1 << index % 8)) != 0;
                    totalArgumentsSize += size;
                    if (isIndirect)
                        totalIndirectArgumentsSize += size;

                    argumentInfos.Add((index, size, isIndirect));
                }
            }

            // Register method for entry/exit hooks
            RegisterMethodHookEntry(functionId, new(
                module.Id,
                typeDef,
                methodDef,
                argumentInfos,
                totalArgumentsSize,
                totalIndirectArgumentsSize,
                request.CaptureArguments,
                request.CaptureReturnValue));
        }

        if (request.Bytecode.Span.Length > 0)
        {
            // Allocate memory for new method body
            var size = request.Bytecode.Span.Length;
            var memory = module.AllocMethodBody((ulong)size);
            if (memory == IntPtr.Zero)
            {
                Logger.LogError($"Could not allocate memory for method body \"{methodName}\"");
                return HResult.E_FAIL;
            }

            // Copy new IL to newly allocated method body
            fixed (byte* ptr = request.Bytecode.Span)
                Buffer.MemoryCopy(ptr, memory.ToPointer(), size, size);

            // Swap method body (discard the original IL and use the instrumented version)
            if (!corProfilerInfo.SetILFunctionBody(module.Id, methodDef, memory))
            {
                Logger.LogError($"Could not set method body for method \"{methodName}\"");
                return HResult.E_FAIL;
            }
        }

        return HResult.S_OK;
    }

    public HResult JITCompilationFinished(FunctionId functionId, HResult hrStatus, bool fIsSafeToBlock)
    {
        return HResult.S_OK;
    }

    public HResult JITCachedFunctionSearchStarted(FunctionId functionId, out bool pbUseCachedFunction)
    {
        pbUseCachedFunction = false;
        return HResult.S_OK;
    }

    public HResult JITCachedFunctionSearchFinished(FunctionId functionId, COR_PRF_JIT_CACHE result)
    {
        return HResult.S_OK;
    }

    public HResult JITFunctionPitched(FunctionId functionId)
    {
        return HResult.S_OK;
    }

    public HResult JITInlining(FunctionId callerId, FunctionId calleeId, out bool pfShouldInline)
    {
        pfShouldInline = false;
        return HResult.S_OK;
    }

    public HResult ThreadCreated(ThreadId threadId)
    {
        messagingClient.SendNotification(messageFactory.CreateThreadCreatedNotification(threadId));
        return HResult.S_OK;
    }

    public HResult ThreadDestroyed(ThreadId threadId)
    {
        messagingClient.SendNotification(messageFactory.CreateThreadDestroyedNotification(threadId));
        return HResult.S_OK;
    }

    public HResult ThreadAssignedToOSThread(ThreadId managedThreadId, int osThreadId)
    {
        return HResult.S_OK;
    }

    public HResult RemotingClientInvocationStarted()
    {
        return HResult.S_OK;
    }

    public HResult RemotingClientSendingMessage(in Guid pCookie, bool fIsAsync)
    {
        return HResult.S_OK;
    }

    public HResult RemotingClientReceivingReply(in Guid pCookie, bool fIsAsync)
    {
        return HResult.S_OK;
    }

    public HResult RemotingClientInvocationFinished()
    {
        return HResult.S_OK;
    }

    public HResult RemotingServerReceivingMessage(in Guid pCookie, bool fIsAsync)
    {
        return HResult.S_OK;
    }

    public HResult RemotingServerInvocationStarted()
    {
        return HResult.S_OK;
    }

    public HResult RemotingServerInvocationReturned()
    {
        return HResult.S_OK;
    }

    public HResult RemotingServerSendingReply(in Guid pCookie, bool fIsAsync)
    {
        return HResult.S_OK;
    }

    public HResult UnmanagedToManagedTransition(FunctionId functionId, COR_PRF_TRANSITION_REASON reason)
    {
        return HResult.S_OK;
    }

    public HResult ManagedToUnmanagedTransition(FunctionId functionId, COR_PRF_TRANSITION_REASON reason)
    {
        return HResult.S_OK;
    }

    public HResult RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON suspendReason)
    {
        var message = messageFactory.CreateRuntimeSuspendStartedNotification(suspendReason);
        messagingClient.SendNotification(message);
        return HResult.S_OK;
    }

    public HResult RuntimeSuspendFinished()
    {
        var message = messageFactory.CreateRuntimeSuspendFinishedNotification();
        messagingClient.SendNotification(message);
        return HResult.S_OK;
    }

    public HResult RuntimeSuspendAborted()
    {
        return HResult.S_OK;
    }

    public HResult RuntimeResumeStarted()
    {
        var message = messageFactory.CreateRuntimeResumeStartedNotification();
        messagingClient.SendNotification(message);
        return HResult.S_OK;
    }

    public HResult RuntimeResumeFinished()
    {
        var message = messageFactory.CreateRuntimeResumeFinishedNotification();
        messagingClient.SendNotification(message);
        return HResult.S_OK;
    }

    public HResult RuntimeThreadSuspended(ThreadId threadId)
    {
        var message = messageFactory.CreateRuntimeThreadSuspendedNotification(threadId);
        messagingClient.SendNotification(message);
        return HResult.S_OK;
    }

    public HResult RuntimeThreadResumed(ThreadId threadId)
    {
        var message = messageFactory.CreateRuntimeThreadResumedNotification(threadId);
        messagingClient.SendNotification(message);
        return HResult.S_OK;
    }

    public HResult MovedReferences(uint cMovedObjectIDRanges, ObjectId* oldObjectIDRangeStart, ObjectId* newObjectIDRangeStart, uint* cObjectIDRangeLength)
    {
        var message = messageFactory.CreateMovedReferencesNotification(
            new ReadOnlySpan<ObjectId>(oldObjectIDRangeStart, unchecked((int)cMovedObjectIDRanges)),
            new ReadOnlySpan<ObjectId>(newObjectIDRangeStart, unchecked((int)cMovedObjectIDRanges)),
            new ReadOnlySpan<uint>(cObjectIDRangeLength, unchecked((int)cMovedObjectIDRanges)));
        messagingClient.SendNotification(message);
        return HResult.S_OK;
    }

    public HResult ObjectAllocated(ObjectId objectId, ClassId classId)
    {
        return HResult.S_OK;
    }

    public HResult ObjectsAllocatedByClass(uint cClassCount, ClassId* classIds, uint* cObjects)
    {
        return HResult.S_OK;
    }

    public HResult ObjectReferences(ObjectId objectId, ClassId classId, uint cObjectRefs, ObjectId* objectRefIds)
    {
        return HResult.S_OK;
    }

    public HResult RootReferences(uint cRootRefs, ObjectId* rootRefIds)
    {
        return HResult.S_OK;
    }

    public HResult ExceptionThrown(ObjectId thrownObjectId)
    {
        return HResult.S_OK;
    }

    public HResult ExceptionSearchFunctionEnter(FunctionId functionId)
    {
        return HResult.S_OK;
    }

    public HResult ExceptionSearchFunctionLeave()
    {
        return HResult.S_OK;
    }

    public HResult ExceptionSearchFilterEnter(FunctionId functionId)
    {
        return HResult.S_OK;
    }

    public HResult ExceptionSearchFilterLeave()
    {
        return HResult.S_OK;
    }

    public HResult ExceptionSearchCatcherFound(FunctionId functionId)
    {
        return HResult.S_OK;
    }

    public HResult ExceptionOSHandlerEnter(nint* __unused)
    {
        return HResult.S_OK;
    }

    public HResult ExceptionOSHandlerLeave(nint* __unused)
    {
        return HResult.S_OK;
    }

    public HResult ExceptionUnwindFunctionEnter(FunctionId functionId)
    {
        return HResult.S_OK;
    }

    public HResult ExceptionUnwindFunctionLeave()
    {
        return HResult.S_OK;
    }

    public HResult ExceptionUnwindFinallyEnter(FunctionId functionId)
    {
        return HResult.S_OK;
    }

    public HResult ExceptionUnwindFinallyLeave()
    {
        return HResult.S_OK;
    }

    public HResult ExceptionCatcherEnter(FunctionId functionId, ObjectId objectId)
    {
        return HResult.S_OK;
    }

    public HResult ExceptionCatcherLeave()
    {
        return HResult.S_OK;
    }

    public HResult COMClassicVTableCreated(ClassId wrappedClassId, in Guid implementedIID, void* pVTable, uint cSlots)
    {
        return HResult.S_OK;
    }

    public HResult COMClassicVTableDestroyed(ClassId wrappedClassId, in Guid implementedIID, void* pVTable)
    {
        return HResult.S_OK;
    }

    public HResult ExceptionCLRCatcherFound()
    {
        return HResult.S_OK;
    }

    public HResult ExceptionCLRCatcherExecute()
    {
        return HResult.S_OK;
    }

    public HResult ThreadNameChanged(ThreadId threadId, uint cchName, char* name)
    {
        return HResult.S_OK;
    }

    public HResult GarbageCollectionStarted(int cGenerations, bool* generationCollected, COR_PRF_GC_REASON reason)
    {
        if (!GetGenerationBounds(out var bounds))
        {
            Logger.LogError($"Could not obtain GC generation ranges during {nameof(GarbageCollectionStarted)}");
            return HResult.E_FAIL;
        }

        var message = messageFactory.CreateGarbageCollectionStartedNotification(
            new ReadOnlySpan<bool>(generationCollected, cGenerations), bounds);
        messagingClient.SendNotification(message);

        return HResult.S_OK;
    }

    private HResult GetGenerationBounds(out COR_PRF_GC_GENERATION_RANGE[]? ranges)
    {
        ranges = null;
        if (!corProfilerInfo.GetGenerationBounds(0, out var cBounds, null))
        {
            ranges = new COR_PRF_GC_GENERATION_RANGE[cBounds];
            fixed (COR_PRF_GC_GENERATION_RANGE* ptr = ranges)
            {
                if (!corProfilerInfo.GetGenerationBounds(cBounds, out _, ptr))
                    return HResult.E_FAIL;
            }
        }

        return HResult.S_OK;
    }

    public HResult SurvivingReferences(uint cSurvivingObjectIDRanges, ObjectId* objectIDRangeStart, uint* cObjectIDRangeLength)
    {
        var message = messageFactory.CreateSurvivingReferencesNotification(
            new ReadOnlySpan<ObjectId>(objectIDRangeStart, (int)cSurvivingObjectIDRanges),
            new ReadOnlySpan<uint>(cObjectIDRangeLength, (int)cSurvivingObjectIDRanges));
        messagingClient.SendNotification(message);
        return HResult.S_OK;
    }

    public HResult GarbageCollectionFinished()
    {
        if (!GetGenerationBounds(out var bounds))
        {
            Logger.LogError($"Could not obtain GC generation ranges during {nameof(GarbageCollectionFinished)}");
            return HResult.E_FAIL;
        }

        // Send notification and wait for request
        var message = messageFactory.CreateGarbageCollectionFinishedNotification(bounds);
        var newNotificationId = messagingClient.GetNewNotificationId();
        var future = messagingClient.ReceiveRequest(newNotificationId);
        messagingClient.SendNotification(message, newNotificationId);

        var request = future.Result;
        if (request.ContinueExecution == null)
            Logger.LogWarning("Unexpected request. Continuing execution...");
        var response = messageFactory.CreateResponse(request, true);
        messagingClient.SendResponse(message);
        return HResult.S_OK;
    }

    public HResult FinalizeableObjectQueued(int finalizerFlags, ObjectId objectID)
    {
        return HResult.S_OK;
    }

    public HResult RootReferences2(uint cRootRefs, ObjectId* rootRefIds, COR_PRF_GC_ROOT_KIND* rootKinds, COR_PRF_GC_ROOT_FLAGS* rootFlags, uint* rootIds)
    {
        return HResult.S_OK;
    }

    public HResult HandleCreated(GCHandleId handleId, ObjectId initialObjectId)
    {
        return HResult.S_OK;
    }

    public HResult HandleDestroyed(GCHandleId handleId)
    {
        return HResult.S_OK;
    }

    public HResult EnterMethod(IntPtr functionIdOrClientId, COR_PRF_ELT_INFO eltInfo)
    {
        var byteArrayPool = ArrayPool<byte>.Shared;
        var functionId = new FunctionId((nuint)functionIdOrClientId);
        var methodHookInfo = methodHooks[functionId];

        // If we do not track arguments, we can just notify about the call itself
        if (!methodHookInfo.CaptureArguments)
        {
            var message = messageFactory.CreateMethodCalledNotification(
                methodHookInfo.ModuleId, 
                methodHookInfo.TypeDef, 
                methodHookInfo.MethodDef);
            messagingClient.SendNotification(message);
        }

        // Get information about arguments
        var cbArgumentInfo = 0UL;
        corProfilerInfo.GetFunctionEnter3Info(functionId, eltInfo, out var frameInfo, &cbArgumentInfo, null);

        // Retrieve spilled argument values from memory
        byte[] argumentValues;
        byte[] argumentOffsets;
        var indirectAddresses = new List<nint>();
        var rawArgumentInfos = byteArrayPool.Rent((int)cbArgumentInfo);
        fixed (byte* ptr = rawArgumentInfos)
        {
            if (!corProfilerInfo.GetFunctionEnter3Info(functionId, eltInfo, out _, &cbArgumentInfo, ptr))
            {
                Logger.LogError("Could not obtain method arguments");
                byteArrayPool.Return(rawArgumentInfos);
                return HResult.E_FAIL;
            }
        }

        // Preprocess arguments (parse values and offsets)
        var argumentInfos = new COR_PRF_FUNCTION_ARGUMENT_INFO(rawArgumentInfos);
        var argumentValuesLength = (int)methodHookInfo.TotalArgumentValuesSize;
        var argumentOffsetsLength = (int)(methodHookInfo.ArgumentInfos.Count * sizeof(uint));
        argumentValues = byteArrayPool.Rent(argumentValuesLength);
        argumentOffsets = byteArrayPool.Rent(argumentOffsetsLength);
        GetArguments(methodHookInfo, indirectAddresses, argumentInfos, argumentValues, argumentOffsets);

        if (methodHookInfo.CaptureReturnValue)
        {
            // Save information about indirects (by-ref args) for method leave callback
            methodHookInfo.PushIndirects(indirectAddresses);
        }

        // Pack information and issue a notification
        var messageWithArguments = messageFactory.CreateMethodCalledWithArgumentsNotification(
            methodHookInfo.ModuleId,
            methodHookInfo.TypeDef,
            methodHookInfo.MethodDef,
            new(argumentValues, 0, argumentValuesLength),
            new(argumentOffsets, 0, argumentOffsetsLength));
        messagingClient.SendNotification(messageWithArguments);

        // Cleanup
        byteArrayPool.Return(rawArgumentInfos);
        byteArrayPool.Return(argumentValues);
        byteArrayPool.Return(argumentOffsets);
        return HResult.S_OK;
    }

    public HResult LeaveMethod(IntPtr functionIdOrClientId, COR_PRF_ELT_INFO eltInfo)
    {
        var byteArrayPool = ArrayPool<byte>.Shared;
        var functionId = new FunctionId((nuint)functionIdOrClientId);
        var methodHookInfo = methodHooks[functionId];

        // If we do not track arguments, we can just notify about the call itself
        if (!methodHookInfo.CaptureReturnValue)
        {
            var message = messageFactory.CreateMethodReturnedNotification(
                methodHookInfo.ModuleId,
                methodHookInfo.TypeDef,
                methodHookInfo.MethodDef);
            messagingClient.SendNotification(message);
        }

        // Get information about return value
        if (!corProfilerInfo.GetFunctionLeave3Info(functionId, eltInfo, out var frameInfo, out var returnValueInfo))
        {
            Logger.LogError("Could not obtain information about method return value");
            return HResult.E_FAIL;
        }

        // Retrieve spilled return value
        var returnValueBytes = byteArrayPool.Rent((int)returnValueInfo.Length);
        fixed (byte* ptr = returnValueBytes)
            Buffer.MemoryCopy(returnValueInfo.StartAddress.ToPointer(), ptr, returnValueInfo.Length, returnValueInfo.Length);

        // Get information about indirects
        var indirects = methodHookInfo.PopIndirects();
        var cbArgumentValues = 0;
        var cbArgumentOffsets = 0;
        byte[]? argumentValues = null;
        byte[]? argumentOffsets = null;
        if (indirects.Count > 0)
        {
            // Copy arguments
            cbArgumentValues = (int)methodHookInfo.TotalIndirectArgumentValuesSize;
            cbArgumentOffsets = indirects.Count * sizeof(uint);
            argumentValues = byteArrayPool.Rent(cbArgumentValues);
            argumentOffsets = byteArrayPool.Rent(cbArgumentOffsets);
            GetByRefArguments(methodHookInfo, indirects, argumentValues, argumentOffsets);
        }

        // Pack information and issue a notification
        var messageWithReturnValue = messageFactory.CreateMethodReturnedWithReturnValueNotification(
            methodHookInfo.ModuleId,
            methodHookInfo.TypeDef,
            methodHookInfo.MethodDef,
            returnValueBytes,
            (argumentValues != null) ? new(argumentValues, 0, cbArgumentValues) : Span<byte>.Empty,
            (argumentOffsets != null) ? new(argumentOffsets, 0, cbArgumentOffsets) : Span<byte>.Empty);
        messagingClient.SendNotification(messageWithReturnValue);

        // Cleanup
        byteArrayPool.Return(returnValueBytes);
        if (argumentValues != null)
            byteArrayPool.Return(argumentValues);
        if (argumentOffsets != null)
            byteArrayPool.Return(argumentOffsets);

        return HResult.S_OK;
    }

    private void GetArguments(
        Method hookInfo, 
        List<nint> indirects, 
        COR_PRF_FUNCTION_ARGUMENT_INFO argumentInfos, 
        Span<byte> argumentValues, 
        Span<byte> argumentOffsets)
    {
        fixed (byte* pArgumentValues = argumentValues)
        {
            fixed (byte* pArgumentOffsets = argumentOffsets)
            {
                // Copy arguments
                var pArgValue = pArgumentValues;
                var pArgOffset = pArgumentOffsets;
                foreach (var (argIndex, argSize, isIndirect) in hookInfo.ArgumentInfos)
                {
                    var range = argumentInfos.GetRange(argIndex);

                    if (isIndirect)
                    {
                        // Get pointer to the value
                        nint pointer = IntPtr.Zero;
                        Buffer.MemoryCopy(range.StartAddress.ToPointer(), &pointer, sizeof(nint), sizeof(nint));
                        indirects.Add(pointer);

                        // Read the value
                        Buffer.MemoryCopy(pointer.ToPointer(), pArgValue, argSize, argSize);
                        var argInfo = (uint)((argIndex << 16) | (argSize));
                        Buffer.MemoryCopy(&argInfo, pArgOffset, sizeof(uint), sizeof(uint));
                        pArgValue += argSize;
                    }
                    else
                    {
                        // Directly read the value
                        var argInfo = (uint)((argIndex << 16) | (int)(range.Length));
                        Buffer.MemoryCopy(range.StartAddress.ToPointer(), pArgValue, range.Length, range.Length);
                        Buffer.MemoryCopy(&argInfo, pArgOffset, sizeof(uint), sizeof(uint));
                        pArgValue += range.Length;
                    }

                    pArgOffset += sizeof(uint);
                }
            }
        }
    }

    private void GetByRefArguments(
        Method hookInfo,
        List<nint> indirects,
        Span<byte> indirectValues,
        Span<byte> indirectOffsets)
    {
        fixed (byte* pArgumentValues = indirectValues)
        {
            fixed (byte* pArgumentOffsets = indirectOffsets)
            {
                // Copy arguments
                var pArgumentValue = pArgumentValues;
                var pArgumentOffset = pArgumentOffsets;
                var indirectsCount = 0;
                foreach (var (argIndex, argSize, isIndirectLoad) in hookInfo.ArgumentInfos.Where(i => i.Item3))
                {
                    var argInfo = (uint)(argIndex << 16) | (argSize);
                    Buffer.MemoryCopy(indirects[indirectsCount].ToPointer(), pArgumentValue, argSize, argSize);
                    Buffer.MemoryCopy(&argInfo, pArgumentOffset, sizeof(uint), sizeof(uint));
                    pArgumentValue += argSize;
                    pArgumentOffset += sizeof(uint);
                    indirectsCount++;
                }
            }
        }
    }

    public HResult TryGetMethodHookEntry(FunctionId functionId, [NotNullWhen(returnValue: default)] out Method? method)
    {
        return methodHooks.TryGetValue(functionId, out method) ? HResult.S_OK : HResult.E_FAIL;
    }

    public HResult RegisterMethodHookEntry(FunctionId functionId, Method method)
    {
        return methodHooks.TryAdd(functionId, method) ? HResult.S_OK : HResult.E_FAIL;
    }

    #region COM_STUFF
    public int QueryInterface(in Guid guid, out nint ptr)
    {
        if (guid == KnownGuids.ICorProfilerCallback2)
        {
            ptr = Object;
            return HResult.S_OK;
        }

        ptr = default;
        return HResult.E_NOINTERFACE;
    }

    public int AddRef()
    {
        return 0;
    }

    public int Release()
    {
        return 0;
    }
    #endregion
}