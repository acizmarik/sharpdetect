using dnlib.DotNet;
using SharpDetect.Common;
using SharpDetect.Common.Exceptions;
using SharpDetect.Common.Interop;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Metadata;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Threads;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Core.Runtime.Memory;
using SharpDetect.Core.Runtime.Threads;
using SharpDetect.Instrumentation.Utilities;
using System.Collections.Concurrent;

namespace SharpDetect.Core.Runtime
{
    internal class ShadowCLR : IShadowCLR
    {
        public int ProcessId { get; }
        public ShadowRuntimeState State { get; private set; }
        public COR_PRF_SUSPEND_REASON? SuspensionReason { get; set; }

        internal readonly ShadowGC ShadowGC;
        internal readonly ConcurrentDictionary<UIntPtr, ShadowThread> Threads;
        internal readonly ConcurrentDictionary<ModuleInfo, ModuleDef> Modules;
        internal readonly ConcurrentDictionary<TypeInfo, TypeDef> Types;
        internal readonly ConcurrentDictionary<FunctionInfo, MethodDef> Functions;
        private readonly IModuleBindContext moduleBindContext;
        private readonly IMetadataResolver resolver;
        private readonly IMetadataEmitter emitter;
        private volatile bool ongoingGarbageCollection;

        public ShadowCLR(int processId, IMetadataResolver resolver, IMetadataEmitter emitter, IModuleBindContext moduleBindContext)
        {
            State = ShadowRuntimeState.Initiated;
            this.ShadowGC = new ShadowGC();
            this.ProcessId = processId;
            this.resolver = resolver;
            this.emitter = emitter;
            this.moduleBindContext = moduleBindContext;

            Threads = new();
            Modules = new();
            Types = new();
            Functions = new();
        }

        public void Process_ProfilerInitialized()
        {
            State = ShadowRuntimeState.Executing;
        }
        
        public void Process_ProfilerDestroyed()
        {
            State = ShadowRuntimeState.Terminated;
        }

        public void Process_ModuleLoaded(ModuleInfo moduleInfo, string path)
        {
            // Fetch module
            var module = moduleBindContext.LoadModule(ProcessId, path, moduleInfo);

            Modules.TryAdd(moduleInfo, module);
        }

        public void Process_TypeLoaded(TypeInfo typeInfo)
        {
            // Fetch module from disk (this should have been already loaded)
            var module = Modules[new ModuleInfo(typeInfo.ModuleId)];
            // Resolve metadata token as a type definition
            var type = (TypeDef)module.ResolveToken(typeInfo.TypeToken);

            Types.TryAdd(typeInfo, type);
        }

        public void Process_JITCompilationStarted(FunctionInfo functionInfo)
        {
            // Fetch module from disk (this should have been already loaded)
            var module = Modules[new ModuleInfo(functionInfo.ModuleId)];
            // Resolve metadata token as a method definition
            var function = (MethodDef)module.ResolveToken(functionInfo.FunctionToken);

            Functions.TryAdd(functionInfo, function);
        }

        public void Process_ThreadCreated(ShadowThread thread)
        {
            Threads.TryAdd(thread.Id, thread);
        }

        public void Process_ThreadDestroyed(ShadowThread thread)
        {
            Threads.TryRemove(new KeyValuePair<UIntPtr, ShadowThread>(thread.Id, thread));
        }

        public void Process_RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON reason)
        {
            { // <Contracts>
                Guard.Equal<ShadowRuntimeState, ShadowRuntimeStateException>(ShadowRuntimeState.Executing, State);
                Guard.Equal<COR_PRF_SUSPEND_REASON?, ShadowRuntimeStateException>(null, SuspensionReason);
            } // </Contracts>

            State = ShadowRuntimeState.Suspended;
            SuspensionReason = reason;
        }

        public void Process_RuntimeSuspendFinished()
        {
            { // <Contracts>
                Guard.Equal<ShadowRuntimeState, ShadowRuntimeStateException>(ShadowRuntimeState.Suspended, State);
                Guard.NotEqual<COR_PRF_SUSPEND_REASON?, ShadowRuntimeStateException>(null, SuspensionReason);
            } // </Contracts>

            State = ShadowRuntimeState.Executing;
            SuspensionReason = null;
        }

        public void Process_RuntimeThreadSuspended(ShadowThread thread)
        {
            { // <Contracts>
                Guard.Equal<ShadowRuntimeState, ShadowRuntimeStateException>(ShadowRuntimeState.Suspended, State);
                Guard.Equal<ShadowThreadState, ShadowRuntimeStateException>(ShadowThreadState.Running, thread.State);
            } // </Contracts>
            thread.EnterState(ShadowThreadState.Suspended);
        }

        public void Process_RuntimeThreadResumed(ShadowThread thread)
        {
            { // <Contracts>
                Guard.Equal<ShadowRuntimeState, ShadowRuntimeStateException>(ShadowRuntimeState.Suspended, State);
                Guard.Equal<ShadowThreadState, ShadowRuntimeStateException>(ShadowThreadState.Suspended, thread.State);
            } // </Contracts>

            thread.EnterState(ShadowThreadState.Running);
        }

        public void Process_GarbageCollectionStarted(bool[] generationsCollected, COR_PRF_GC_GENERATION_RANGE[] bounds)
        {
            { // <Contracts>
                Guard.Equal<ShadowRuntimeState, ShadowRuntimeStateException>(ShadowRuntimeState.Suspended, State);
                Guard.Equal<COR_PRF_SUSPEND_REASON?, ShadowRuntimeStateException>(COR_PRF_SUSPEND_REASON.GC, SuspensionReason);
                Guard.False<ShadowRuntimeStateException>(ongoingGarbageCollection);
            } // </Contracts>

            ShadowGC.ProcessGarbageCollectionStarted(bounds, generationsCollected);
            ongoingGarbageCollection = true;
        }

        public void Process_GarbageCollectionFinished(COR_PRF_GC_GENERATION_RANGE[] bounds)
        {
            { // <Contracts>
                Guard.Equal<ShadowRuntimeState, ShadowRuntimeStateException>(ShadowRuntimeState.Suspended, State);
                Guard.Equal<COR_PRF_SUSPEND_REASON?, ShadowRuntimeStateException>(COR_PRF_SUSPEND_REASON.GC, SuspensionReason);
                Guard.True<ShadowRuntimeStateException>(ongoingGarbageCollection);
            } // </Contracts>

            ShadowGC.ProcessGarbageCollectionFinished(bounds);
            ongoingGarbageCollection = false;
        }

        public void Process_SurvivingReferences(UIntPtr[] starts, UIntPtr[] lengths)
        {
            { // <Contracts>
                Guard.Equal<ShadowRuntimeState, ShadowRuntimeStateException>(ShadowRuntimeState.Suspended, State);
                Guard.Equal<COR_PRF_SUSPEND_REASON?, ShadowRuntimeStateException>(COR_PRF_SUSPEND_REASON.GC, SuspensionReason);
                Guard.True<ShadowRuntimeStateException>(ongoingGarbageCollection);
            } // </Contracts>

            ShadowGC.ProcessSurvivingReferences(starts, lengths);
        }

        public void Process_MovedReferences(UIntPtr[] oldStarts, UIntPtr[] newStarts, UIntPtr[] lengths)
        {
            { // <Contracts>
                Guard.Equal<ShadowRuntimeState, ShadowRuntimeStateException>(ShadowRuntimeState.Suspended, State);
                Guard.Equal<COR_PRF_SUSPEND_REASON?, ShadowRuntimeStateException>(COR_PRF_SUSPEND_REASON.GC, SuspensionReason);
                Guard.True<ShadowRuntimeStateException>(ongoingGarbageCollection);
            } // </Contracts>

            ShadowGC.ProcessMovedReferences(oldStarts, newStarts, lengths);
        }

        public void Process_TypeInjected(TypeInfo typeInfo)
        {
            var moduleInfo = new ModuleInfo(typeInfo.ModuleId);
            var injectedModule = default(ModuleDef);
            { // <Contracts>
                Guard.True<ShadowRuntimeStateException>(resolver.TryGetModuleDef(moduleInfo, out injectedModule));
                Guard.NotNull<ModuleDef, ShadowRuntimeStateException>(injectedModule);
            } // </Contracts>

            // Generate helper type
            var helper = MetadataGenerator.CreateHelperType(injectedModule.CorLibTypes);

            // Emit helper
            emitter.Emit(moduleInfo, helper, typeInfo.TypeToken);
        }

        public void Process_MethodInjected(FunctionInfo functionInfo, MethodType type)
        {
            var moduleInfo = new ModuleInfo(functionInfo.ModuleId);
            var injectedModule = default(ModuleDef);
            var injectedType = default(TypeDef);
            { // <Contracts>
                Guard.True<ShadowRuntimeStateException>(resolver.TryGetModuleDef(moduleInfo, out injectedModule));
                Guard.True<ShadowRuntimeStateException>(resolver.TryGetTypeDef(new(functionInfo.ModuleId, functionInfo.TypeToken), new(functionInfo.ModuleId), out injectedType));
                Guard.NotNull<ModuleDef, ShadowRuntimeStateException>(injectedModule);
                Guard.NotNull<TypeDef, ShadowRuntimeStateException>(injectedType);
            } // </Contracts>

            // Generate helper method
            var helper = MetadataGenerator.CreateHelperMethod(injectedType, type, injectedModule.CorLibTypes);

            // Emit helper
            emitter.Emit(moduleInfo, helper, functionInfo.FunctionToken);
        }

        public void Process_MethodWrapped(FunctionInfo functionInfo, MDToken wrapperToken)
        {
            var wrappedMethod = default(MethodDef);
            { // <Contracts>
                Guard.True<ShadowRuntimeStateException>(resolver.TryGetMethodDef(functionInfo, new ModuleInfo(functionInfo.ModuleId), resolveWrappers: false, out wrappedMethod));
                Guard.NotNull<MethodDef, ShadowRuntimeStateException>(wrappedMethod);
            } // </Contracts>

            // Generate method wrapper
            var wrapper = MetadataGenerator.CreateWrapper(wrappedMethod);

            // Emit and bind wrapper to the original method
            emitter.Emit(new(functionInfo.ModuleId), wrapper, wrapperToken);
            emitter.Bind(new(wrappedMethod), new(wrapper), new(functionInfo.ModuleId, functionInfo.TypeToken, wrapperToken));
        }

        public void Process_TypeReferenced(TypeInfo typeInfo)
        {
            // TODO: do we need this for anything?
        }

        public void Process_HelperMethodReferenced(FunctionInfo functionRef, MethodType type)
        {
            // Bind reference to the original definition
            emitter.Bind(type, functionRef);
        }

        public void Process_WrapperMethodReferenced(FunctionInfo functionDef, FunctionInfo functionRef)
        {
            var wrapperMethod = default(WrapperMethodDef);
            var wrappedMethod = default(MethodDef);
            { // <Contracts>
                // Resolve wrapper method reference
                Guard.True<ShadowRuntimeStateException>(resolver.TryGetMethodDef(functionDef, new ModuleInfo(functionDef.ModuleId), resolveWrappers: false, out wrappedMethod));
                Guard.NotNull<MethodDef, ShadowRuntimeStateException>(wrappedMethod);

                // Resolve wrapper method definition
                Guard.True<ShadowRuntimeStateException>(resolver.TryGetWrapperMethodReference(wrappedMethod, new(functionDef.ModuleId), out var wrapperReference));
                Guard.True<ShadowRuntimeStateException>(resolver.TryGetWrapperMethodDefinition(wrapperReference, new(functionDef.ModuleId), out wrapperMethod));                
            } // </Contract>

            // Bind reference to the original definition
            emitter.Bind(new(wrappedMethod), wrapperMethod, functionRef);
        }
    }
}
