using SharpDetect.Common.Messages;
using SharpDetect.Profiler.Communication;
using SharpDetect.Profiler.Hooks;
using SharpDetect.Profiler.Logging;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Profiler;

internal unsafe class CorProfilerCallback : ICorProfilerCallback2
{
    public NativeObjects.ICorProfilerCallback2 Object { get; private set; }
    public static CorProfilerCallback? Instance { get; private set; }

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
        return HResult.S_OK;
    }

    public HResult SurvivingReferences(uint cSurvivingObjectIDRanges, ObjectId* objectIDRangeStart, uint* cObjectIDRangeLength)
    {
        return HResult.S_OK;
    }

    public HResult GarbageCollectionFinished()
    {
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

    public HResult TryGetMethodHookEntry(FunctionId functionId, [NotNullWhen(returnValue: default)] out Method? method)
    {
        return methodHooks.TryGetValue(functionId, out method) ? HResult.S_OK : HResult.E_FAIL;
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