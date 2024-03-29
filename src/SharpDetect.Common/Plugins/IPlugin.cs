﻿// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Arguments;
using SharpDetect.Common.Runtime.Threads;

namespace SharpDetect.Common.Plugins
{
    public interface IPlugin
    {
        void Configure(IConfiguration configuration);

        void AnalysisStarted(EventInfo info);
        void AnalysisEnded(EventInfo info);

        void ModuleLoaded(ModuleInfo module, string path, EventInfo info);
        void TypeLoaded(TypeInfo type, EventInfo info);
        void JITCompilationStarted(FunctionInfo method, EventInfo info);

        void ThreadCreated(IShadowThread thread, EventInfo info);
        void ThreadDestroyed(IShadowThread thread, EventInfo info);

        void GarbageCollectionStarted(EventInfo info);
        void GarbageCollectionFinished(EventInfo info);

        void FieldRead(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info);
        void FieldWritten(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info);
        void ArrayElementRead(ulong srcMappingId, IShadowObject instance, int index, EventInfo info);
        void ArrayElementWritten(ulong srcMappingId, IShadowObject instance, int index, EventInfo info);

        void MethodCalled(FunctionInfo method, IArgumentsList? arguments, EventInfo info);
        void MethodReturned(FunctionInfo method, IValueOrObject? returnValue, IArgumentsList? byRefArguments, EventInfo info);

        void LockAcquireAttempted(IShadowObject instance, EventInfo info);
        void LockAcquireReturned(IShadowObject instance, bool isSuccess, EventInfo info);
        void LockReleased(IShadowObject instance, EventInfo info);

        void ObjectWaitCalled(IShadowObject instance, EventInfo info);
        void ObjectWaitReturned(IShadowObject instance, bool isSuccess, EventInfo info);
        void ObjectPulsed(IShadowObject instance, bool isPulseAll, EventInfo info);
    }
}
