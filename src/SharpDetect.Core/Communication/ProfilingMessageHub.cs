using Microsoft.Extensions.Logging;
using SharpDetect.Common;
using SharpDetect.Common.Interop;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Services;
using SharpDetect.Common.Unsafe;

namespace SharpDetect.Core.Communication
{
    internal class ProfilingMessageHub : MessageHubBase, IProfilingMessageHub
    {
        public event Action<EventInfo>? Heartbeat;
        public event Action<(Version? Version, EventInfo Info)>? ProfilerLoaded;
        public event Action<EventInfo>? ProfilerInitialized;
        public event Action<EventInfo>? ProfilerDestroyed;
        public event Action<(UIntPtr ModuleId, string Path, EventInfo Info)>? ModuleLoaded;
        public event Action<(TypeInfo TypeInfo, EventInfo Info)>? TypeLoaded;
        public event Action<(FunctionInfo FunctionInfo, EventInfo Info)>? JITCompilationStarted;
        public event Action<(UIntPtr ThreadId, EventInfo Info)>? ThreadCreated;
        public event Action<(UIntPtr ThreadId, EventInfo Info)>? ThreadDestroyed;
        public event Action<(COR_PRF_SUSPEND_REASON Reason, EventInfo Info)>? RuntimeSuspendStarted;
        public event Action<EventInfo>? RuntimeSuspendFinished;
        public event Action<(UIntPtr ThreadId, EventInfo Info)>? RuntimeThreadSuspended;
        public event Action<(UIntPtr ThreadId, EventInfo Info)>? RuntimeThreadResumed;
        public event Action<(bool[] GenerationsCollected, COR_PRF_GC_GENERATION_RANGE[] Bounds, EventInfo Info)>? GarbageCollectionStarted;
        public event Action<(COR_PRF_GC_GENERATION_RANGE[] Bounds, EventInfo Info)>? GarbageCollectionFinished;
        public event Action<(UIntPtr[] BlockStarts, UIntPtr[] Lengths, EventInfo Info)>? SurvivingReferences;
        public event Action<(UIntPtr[] OldBlockStarts, UIntPtr[] NewBlockStarts, UIntPtr[] Lengths, EventInfo Info)>? MovedReferences;

        public ProfilingMessageHub(ILoggerFactory loggerFactory)
            : base(loggerFactory.CreateLogger<ProfilingMessageHub>(), new[]
            {
                NotifyMessage.PayloadOneofCase.Heartbeat,
                NotifyMessage.PayloadOneofCase.ProfilerLoaded,
                NotifyMessage.PayloadOneofCase.ProfilerInitialized,
                NotifyMessage.PayloadOneofCase.ProfilerDestroyed,
                NotifyMessage.PayloadOneofCase.ModuleLoaded,
                NotifyMessage.PayloadOneofCase.TypeLoaded,
                NotifyMessage.PayloadOneofCase.JITCompilationStarted,
                NotifyMessage.PayloadOneofCase.ThreadCreated,
                NotifyMessage.PayloadOneofCase.ThreadDestroyed,
                NotifyMessage.PayloadOneofCase.RuntimeSuspendStarted,
                NotifyMessage.PayloadOneofCase.RuntimeSuspendFinished,
                NotifyMessage.PayloadOneofCase.RuntimeThreadSuspended,
                NotifyMessage.PayloadOneofCase.RuntimeThreadResumed,
                NotifyMessage.PayloadOneofCase.GarbageCollectionStarted,
                NotifyMessage.PayloadOneofCase.GarbageCollectionFinished,
                NotifyMessage.PayloadOneofCase.RuntimeSuspendFinished,
                NotifyMessage.PayloadOneofCase.SurvivingReferences,
                NotifyMessage.PayloadOneofCase.MovedReferences
            })
        {
        }

        public void Process(NotifyMessage message)
        {
            switch (message.PayloadCase)
            {
                case NotifyMessage.PayloadOneofCase.Heartbeat: DispatchHeartbeat(message); break;
                case NotifyMessage.PayloadOneofCase.ProfilerLoaded: DispatchProfilerLoaded(message); break;
                case NotifyMessage.PayloadOneofCase.ProfilerInitialized: DispatchProfilerInitialized(message); break;
                case NotifyMessage.PayloadOneofCase.ProfilerDestroyed: DispatchProfilerDestroyed(message); break;
                case NotifyMessage.PayloadOneofCase.ModuleLoaded: DispatchModuleLoaded(message); break;
                case NotifyMessage.PayloadOneofCase.TypeLoaded: DispatchTypeLoaded(message); break;
                case NotifyMessage.PayloadOneofCase.JITCompilationStarted: DispatchJITCompilationStarted(message); break;
                case NotifyMessage.PayloadOneofCase.ThreadCreated: DispatchThreadCreated(message); break;
                case NotifyMessage.PayloadOneofCase.ThreadDestroyed: DispatchThreadDestroyed(message); break;
                case NotifyMessage.PayloadOneofCase.RuntimeSuspendStarted: DispatchRuntimeSuspendStarted(message); break;
                case NotifyMessage.PayloadOneofCase.RuntimeSuspendFinished: DispatchRuntimeSuspendFinished(message); break;
                case NotifyMessage.PayloadOneofCase.RuntimeThreadSuspended: DispatchRuntimeThreadSuspended(message); break;
                case NotifyMessage.PayloadOneofCase.RuntimeThreadResumed: DispatchRuntimeThreadResumed(message); break;
                case NotifyMessage.PayloadOneofCase.GarbageCollectionStarted: DispatchGarbageCollectionStarted(message); break;
                case NotifyMessage.PayloadOneofCase.GarbageCollectionFinished: DispatchGarbageCollectionFinished(message); break;
                case NotifyMessage.PayloadOneofCase.SurvivingReferences: DispatchSurvivingReferences(message); break;
                case NotifyMessage.PayloadOneofCase.MovedReferences: DispatchMovedReferences(message); break;

                default:
                    Logger.LogError("[{class}] Unrecognized message type: {type}!", nameof(ProfilingMessageHub), message.PayloadCase);
                    throw new NotSupportedException("Provided message type is not supported.");
            }
        }

        private void DispatchHeartbeat(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            Heartbeat?.Invoke(info);
        }

        private void DispatchProfilerLoaded(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
#pragma warning disable CA1806 // Do not ignore method results
            // Note: version checking should be handled separately, we just dont want exceptions here
            Version.TryParse(message.ProfilerLoaded.Version, out var version);
#pragma warning restore CA1806 // Do not ignore method results
            ProfilerLoaded?.Invoke((version, info));
        }

        private void DispatchProfilerInitialized(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            ProfilerInitialized?.Invoke(info);
        }

        private void DispatchProfilerDestroyed(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            ProfilerDestroyed?.Invoke(info);
        }

        private void DispatchModuleLoaded(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            var moduleLoaded = message.ModuleLoaded;
            ModuleLoaded?.Invoke((new(moduleLoaded.ModuleId), moduleLoaded.ModulePath, info));
        }

        private void DispatchTypeLoaded(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            var typeLoaded = message.TypeLoaded;
            var typeInfo = new TypeInfo(new(typeLoaded.ModuleId), new(typeLoaded.TypeToken));
            TypeLoaded?.Invoke((typeInfo, info));
        }

        private void DispatchJITCompilationStarted(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            var compilationStarted = message.JITCompilationStarted;
            var functionInfo = new FunctionInfo(new(compilationStarted.ModuleId), new(compilationStarted.TypeToken), new(compilationStarted.FunctionToken));
            JITCompilationStarted?.Invoke((functionInfo, info));
        }

        private void DispatchThreadCreated(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            var threadCreated = message.ThreadCreated;
            ThreadCreated?.Invoke((new(threadCreated.ThreadId), info));
        }

        private void DispatchThreadDestroyed(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            var threadDestroyed = message.ThreadDestroyed;
            ThreadDestroyed?.Invoke((new(threadDestroyed.ThreadId), info));
        }

        private void DispatchRuntimeSuspendStarted(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            var runtimeSuspendStarted = message.RuntimeSuspendStarted;
            RuntimeSuspendStarted?.Invoke(((COR_PRF_SUSPEND_REASON)runtimeSuspendStarted.Reason, info));
        }

        private void DispatchRuntimeSuspendFinished(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            RuntimeSuspendFinished?.Invoke(info);
        }

        private void DispatchRuntimeThreadSuspended(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            var runtimeThreadSuspended = message.RuntimeThreadSuspended;
            RuntimeThreadSuspended?.Invoke((new(runtimeThreadSuspended.ThreadId), info));
        }

        private void DispatchRuntimeThreadResumed(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            var runtimeThreadResumed = message.RuntimeThreadResumed;
            RuntimeThreadResumed?.Invoke((new(runtimeThreadResumed.ThreadId), info));
        }

        private void DispatchGarbageCollectionStarted(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            var gcStarted = message.GarbageCollectionStarted;
            var generations = UnsafeHelpers.AsStructArray<bool>(gcStarted.GenerationsCollected.Span);
            var bounds = UnsafeHelpers.AsStructArray<COR_PRF_GC_GENERATION_RANGE>(gcStarted.GenerationSegmentBounds.Span);
            GarbageCollectionStarted?.Invoke((generations, bounds, info));
        }

        private void DispatchGarbageCollectionFinished(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            var gcFinished = message.GarbageCollectionFinished;
            var bounds = UnsafeHelpers.AsStructArray<COR_PRF_GC_GENERATION_RANGE>(gcFinished.GenerationSegmentBounds.Span);
            GarbageCollectionFinished?.Invoke((bounds, info));
        }

        private void DispatchSurvivingReferences(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            var survivingReferences = message.SurvivingReferences;
            var blockStarts = UnsafeHelpers.AsStructArray<UIntPtr>(survivingReferences.Blocks.Span);
            var blockLengths = UnsafeHelpers.AsStructArray<UIntPtr>(survivingReferences.Lengths.Span);
            SurvivingReferences?.Invoke((blockStarts, blockLengths, info));
        }

        private void DispatchMovedReferences(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            var movedReferences = message.MovedReferences;
            var oldBlockStarts = UnsafeHelpers.AsStructArray<UIntPtr>(movedReferences.OldBlocks.Span);
            var newBlockStarts = UnsafeHelpers.AsStructArray<UIntPtr>(movedReferences.NewBlocks.Span);
            var blockLengths = UnsafeHelpers.AsStructArray<UIntPtr>(movedReferences.Lengths.Span);
            MovedReferences?.Invoke((oldBlockStarts, newBlockStarts, blockLengths, info));
        }
    }
}
