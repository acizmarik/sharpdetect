// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using Microsoft.Extensions.Logging;
using SharpDetect.Common;
using SharpDetect.Common.Exceptions;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Threads;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Core.Runtime.Memory;
using SharpDetect.Core.Runtime.Threads;
using SharpDetect.Instrumentation.Utilities;
using SharpDetect.Profiler;
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
        private readonly IModuleBindContext moduleBindContext;
        private readonly IMetadataResolver resolver;
        private readonly IMetadataEmitter emitter;
        private volatile bool ongoingGarbageCollection;

        public ShadowCLR(int processId, IMetadataResolver resolver, IMetadataEmitter emitter, IModuleBindContext moduleBindContext, ILoggerFactory loggerFactory)
        {
            State = ShadowRuntimeState.Initiated;
            this.ShadowGC = new ShadowGC(loggerFactory);
            this.ProcessId = processId;
            this.resolver = resolver;
            this.emitter = emitter;
            this.moduleBindContext = moduleBindContext;

            Threads = new();
            Modules = new();
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
            if (!moduleBindContext.TryLoadModule(ProcessId, path, moduleInfo, out var module))
                return;

            Modules.TryAdd(moduleInfo, module);
        }

        public void Process_TypeLoaded(TypeInfo typeInfo)
        {
            /* Do nothing */
        }

        public void Process_JITCompilationStarted(FunctionInfo functionInfo)
        {
            /* Do nothing */
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
            RuntimeContract.Assert(State == ShadowRuntimeState.Executing);
            RuntimeContract.Assert(SuspensionReason == null);

            State = ShadowRuntimeState.Suspending;
            SuspensionReason = reason;
        }

        public void Process_RuntimeSuspendFinished()
        {
            RuntimeContract.Assert(State == ShadowRuntimeState.Suspending);
            RuntimeContract.Assert(SuspensionReason != null);

            State = ShadowRuntimeState.Suspended;
        }

        public void Process_RuntimeResumeStarted()
        {
            RuntimeContract.Assert(State == ShadowRuntimeState.Suspended);
            RuntimeContract.Assert(SuspensionReason != null);

            State = ShadowRuntimeState.Resuming;
        }

        public void Process_RuntimeResumeFinished()
        {
            RuntimeContract.Assert(State == ShadowRuntimeState.Resuming);
            RuntimeContract.Assert(SuspensionReason != null);

            State = ShadowRuntimeState.Executing;
            SuspensionReason = null;
        }

        public void Process_RuntimeThreadSuspended(ShadowThread thread)
        {
            thread.EnterState(ShadowThreadState.Suspended);
        }

        public void Process_RuntimeThreadResumed(ShadowThread thread)
        {
            thread.EnterState(ShadowThreadState.Running);
        }

        public void Process_GarbageCollectionStarted(bool[] generationsCollected, COR_PRF_GC_GENERATION_RANGE[] bounds)
        {
            RuntimeContract.Assert(State == ShadowRuntimeState.Suspended);
            RuntimeContract.Assert(SuspensionReason == COR_PRF_SUSPEND_REASON.COR_PRF_SUSPEND_FOR_GC);
            RuntimeContract.Assert(!ongoingGarbageCollection);

            ShadowGC.ProcessGarbageCollectionStarted(bounds, generationsCollected);
            ongoingGarbageCollection = true;
        }

        public void Process_GarbageCollectionFinished(COR_PRF_GC_GENERATION_RANGE[] bounds)
        {
            RuntimeContract.Assert(State == ShadowRuntimeState.Suspended);
            RuntimeContract.Assert(SuspensionReason == COR_PRF_SUSPEND_REASON.COR_PRF_SUSPEND_FOR_GC);
            RuntimeContract.Assert(ongoingGarbageCollection);

            ShadowGC.ProcessGarbageCollectionFinished(bounds);
            ongoingGarbageCollection = false;
        }

        public void Process_SurvivingReferences(UIntPtr[] starts, uint[] lengths)
        {
            RuntimeContract.Assert(State == ShadowRuntimeState.Suspended);
            RuntimeContract.Assert(SuspensionReason == COR_PRF_SUSPEND_REASON.COR_PRF_SUSPEND_FOR_GC);
            RuntimeContract.Assert(ongoingGarbageCollection);

            ShadowGC.ProcessSurvivingReferences(starts, lengths);
        }

        public void Process_MovedReferences(UIntPtr[] oldStarts, UIntPtr[] newStarts, uint[] lengths)
        {
            RuntimeContract.Assert(State == ShadowRuntimeState.Suspended);
            RuntimeContract.Assert(SuspensionReason == COR_PRF_SUSPEND_REASON.COR_PRF_SUSPEND_FOR_GC);
            RuntimeContract.Assert(ongoingGarbageCollection);

            ShadowGC.ProcessMovedReferences(oldStarts, newStarts, lengths);
        }

        public void Process_TypeInjected(TypeInfo typeInfo)
        {
            var moduleInfo = new ModuleInfo(typeInfo.ModuleId);
            if (!resolver.TryGetModuleDef(moduleInfo, out var injectedModule))
                ShadowRuntimeStateException.Throw("Could not resolve module");
            RuntimeContract.Assert(injectedModule != null);

            // Generate helper type
            var helper = MetadataGenerator.CreateHelperType(injectedModule.CorLibTypes);

            // Emit helper
            emitter.Emit(moduleInfo, helper, typeInfo.TypeToken);
        }

        public void Process_MethodInjected(FunctionInfo functionInfo, MethodType type)
        {
            var moduleInfo = new ModuleInfo(functionInfo.ModuleId);
            if (!resolver.TryGetModuleDef(moduleInfo, out var injectedModule))
                ShadowRuntimeStateException.Throw("Could not resolve module");
            if (!resolver.TryGetTypeDef(new(functionInfo.ModuleId, functionInfo.TypeToken), new(functionInfo.ModuleId), out var injectedType))
                ShadowRuntimeStateException.Throw("Could not resolve type");
            RuntimeContract.Assert(injectedModule != null);
            RuntimeContract.Assert(injectedType != null);

            // Generate helper method
            var helper = MetadataGenerator.CreateHelperMethod(injectedType, type, injectedModule.CorLibTypes);

            // Emit helper
            emitter.Emit(moduleInfo, helper, functionInfo.FunctionToken);
            emitter.Bind(type, functionInfo);
        }

        public void Process_MethodWrapped(FunctionInfo functionInfo, MDToken wrapperToken)
        {
            var moduleInfo = new ModuleInfo(functionInfo.ModuleId);
            if (!resolver.TryGetMethodDef(functionInfo, moduleInfo, resolveWrappers: true, out var wrappedMethod))
                ShadowRuntimeStateException.Throw("Could not resolve method");
            RuntimeContract.Assert(wrappedMethod != null);

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
            if (!resolver.TryGetMethodDef(functionDef, new ModuleInfo(functionDef.ModuleId), resolveWrappers: true, out var wrappedMethod))
                ShadowRuntimeStateException.Throw("Could not resolve wrapped method.");
            if (!resolver.TryGetWrapperMethodReference(wrappedMethod, new(functionDef.ModuleId), out var wrapperReference))
                ShadowRuntimeStateException.Throw("Could not resolve reference to method wrapper.");
            if (!resolver.TryGetWrapperMethodDefinition(wrapperReference, new(functionDef.ModuleId), out var wrapperMethod))
                ShadowRuntimeStateException.Throw("Could not resolve wrapper method.");

            // Bind reference to the original definition
            emitter.Bind(new(wrappedMethod), wrapperMethod, functionRef);
        }
    }
}
