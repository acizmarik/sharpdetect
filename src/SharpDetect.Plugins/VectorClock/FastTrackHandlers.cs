using dnlib.DotNet;
using SharpDetect.Common;
using SharpDetect.Common.Plugins;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Threads;
using SharpDetect.Common.Services.Instrumentation;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Common.Services.Reporting;
using SharpDetect.Plugins.Utilities;
using System.Collections.Concurrent;

namespace SharpDetect.Plugins.VectorClock
{
    public partial class FastTrackPlugin
    {
        private readonly ConcurrentDictionary<IShadowThread, ThreadState> threads;
        private readonly ConcurrentDictionary<IShadowObject, LockState> locks;
        private readonly ConcurrentDictionary<IShadowObject, ConcurrentDictionary<FieldDef, VariableState>> instanceFields;
        private readonly ConcurrentDictionary<IShadowObject, ConcurrentDictionary<int, VariableState>> arrayElements;
        private readonly ConcurrentDictionary<FieldDef, VariableState> staticFields;
        private readonly object ftLock;
        private volatile int lastThreadId;
        private volatile int threadsCount;

        private readonly IReportingService reportingService;
        private readonly IMetadataContext metadataContext;
        private readonly IEventDescriptorRegistry eventRegistry;
        private TypeDef? threadStaticAttribute;

        public FastTrackPlugin(
            IMetadataContext metadataContext,
            IReportingService reportingService,
            IEventDescriptorRegistry eventRegistry)
        {
            this.reportingService = reportingService;
            this.metadataContext = metadataContext;
            this.eventRegistry = eventRegistry;

            this.threads = new();
            this.locks = new();
            this.instanceFields = new();
            this.arrayElements = new();
            this.staticFields = new();
            this.ftLock = new();
        }

        private ThreadState GetThreadState(IShadowThread thread)
        {
            var threadState = threads.GetOrAdd(thread, _ => new(thread.VirtualId));
            threadState.IncrementEpoch();
            return threadState;
        }

        private LockState GetLockState(ThreadState thread, IShadowObject lockObj)
        {
            return locks.GetOrAdd(lockObj, _ =>
            {
                var clockSize = thread.Clock.Count + 1;
                var clock = new List<uint>(clockSize);
                NewVectorClock(clock, clockSize);
                return LockState.Create(clock);
            });
        }


        private VariableState GetArrayElementVariableState(ThreadState thread, bool isWrite, IShadowObject arrayInstance, int index)
        {
            // Get all tracked variables for the given array
            var trackedArrayElements = arrayElements.GetOrAdd(arrayInstance, _ => new());
            // Get the variable determined by the given index
            if (!trackedArrayElements.ContainsKey(index))
            {
                if (isWrite)
                    trackedArrayElements.TryAdd(index, VariableState.InitFromWrite(thread));
                else
                    trackedArrayElements.TryAdd(index, VariableState.InitFromRead(thread));
            }

            var variable = trackedArrayElements[index];
            return variable;
        }

        private VariableState GetFieldVariableState(ThreadState thread, bool isWrite, FieldDef field, IShadowObject? instance = null)
        {
            // Static field
            if (instance == null)
            {
                var variableState = (isWrite) ? VariableState.InitFromWrite(thread) : VariableState.InitFromRead(thread);
                var trackedStaticField = staticFields.GetOrAdd(field, variableState);
                return trackedStaticField;
            }

            // Instance field
            if (!instanceFields.TryGetValue(instance, out var trackedInstanceFields))
            {
                trackedInstanceFields = new();
                instanceFields.TryAdd(instance, trackedInstanceFields);
            }

            if (!trackedInstanceFields.ContainsKey(field))
            {
                if (isWrite)
                    trackedInstanceFields.TryAdd(field, VariableState.InitFromWrite(thread));
                else if (!isWrite)
                    trackedInstanceFields.TryAdd(field, VariableState.InitFromRead(thread));
            }

            var variable = trackedInstanceFields[field];
            return variable;
        }

        public override void ModuleLoaded(ModuleInfo module, string path, EventInfo info)
        {
            if (threadStaticAttribute == null)
            {
                var resolver = metadataContext.GetResolver(info.Runtime.ProcessId);
                resolver.TryLookupTypeDef("System.ThreadStaticAttribute", module, out threadStaticAttribute);
            }
        }

        public override void ArrayElementRead(ulong srcMappingId, IShadowObject instance, int index, EventInfo info)
        {
            lock (ftLock)
            {
                var thread = GetThreadState(info.Thread);
                var variable = GetArrayElementVariableState(thread, false, instance, index);
                if (!Read(variable, thread))
                {
                    var sourceLink = eventRegistry.Get(srcMappingId);
                    reportingService.CreateReport(
                        plugin: nameof(FastTrackPlugin),
                        messageFormat: DiagnosticsMessageFormatArrays,
                        arguments: new object[] { instance, index },
                        category: DiagnosticsCategory,
                        processId: info.Runtime.ProcessId,
                        thread: info.Thread,
                        sourceLink);
                }
            }
        }

        public override void ArrayElementWritten(ulong srcMappingId, IShadowObject instance, int index, EventInfo info)
        {
            lock (ftLock)
            {
                var thread = GetThreadState(info.Thread);
                var variable = GetArrayElementVariableState(thread, true, instance, index);
                if (!Write(variable, thread))
                {
                    var sourceLink = eventRegistry.Get(srcMappingId);
                    reportingService.CreateReport(
                        plugin: nameof(FastTrackPlugin),
                        messageFormat: DiagnosticsMessageFormatArrays,
                        arguments: new object[] { instance, index },
                        category: DiagnosticsCategory,
                        processId: info.Runtime.ProcessId,
                        thread: info.Thread,
                        sourceLink);
                }
            }
        }

        public override void FieldRead(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info)
        {
            var sourceLink = eventRegistry.Get(srcMappingId);
            var fieldRef = (IField)sourceLink.Instruction.Operand;
            var resolver = metadataContext.GetResolver(info.Runtime.ProcessId);
            if (!resolver.TryResolveFieldDef(fieldRef, out var fieldDef) || !fieldDef.ShouldAnalyzeForDataRaces(threadStaticAttribute!))
                return;

            lock (ftLock)
            {
                var thread = GetThreadState(info.Thread);
                var variable = GetFieldVariableState(thread, false, fieldDef, instance);
                if (!Read(variable, thread))
                {
                    reportingService.CreateReport(
                        plugin: nameof(FastTrackPlugin),
                        messageFormat: DiagnosticsMessageFormatFields,
                        arguments: new[] { fieldDef },
                        category: DiagnosticsCategory,
                        processId: info.Runtime.ProcessId,
                        thread: info.Thread,
                        sourceLink);
                }
            }
        }

        public override void FieldWritten(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info)
        {
            var sourceLink = eventRegistry.Get(srcMappingId);
            var fieldRef = (IField)sourceLink.Instruction.Operand;
            var resolver = metadataContext.GetResolver(info.Runtime.ProcessId);
            if (!resolver.TryResolveFieldDef(fieldRef, out var fieldDef) || !fieldDef.ShouldAnalyzeForDataRaces(threadStaticAttribute!))
                return;

            lock (ftLock)
            {
                var thread = GetThreadState(info.Thread);
                var variable = GetFieldVariableState(thread, true, fieldDef, instance);
                if (!Write(variable, thread))
                {
                    reportingService.CreateReport(
                        plugin: nameof(FastTrackPlugin),
                        messageFormat: DiagnosticsMessageFormatFields,
                        arguments: new[] { fieldDef },
                        category: DiagnosticsCategory,
                        processId: info.Runtime.ProcessId,
                        thread: info.Thread,
                        sourceLink);
                }
            }
        }

        public override void LockAcquireReturned(IShadowObject instance, bool isSuccess, EventInfo info)
        {
            if (isSuccess)
            {
                lock (ftLock)
                {
                    var thread = GetThreadState(info.Thread);
                    var @lock = GetLockState(thread, instance);
                    Acquire(thread, @lock);
                }
            }
        }

        public override void LockReleased(IShadowObject instance, EventInfo info)
        {
            lock (ftLock)
            {
                var thread = GetThreadState(info.Thread);
                var @lock = GetLockState(thread, instance);
                Release(thread, @lock);
            }
        }

        public override void ThreadCreated(IShadowThread thread, EventInfo info)
        {
            lock (ftLock)
            {
                var originalThread = GetThreadState(info.Thread);
                var newThread = GetThreadState(thread);
                Fork(originalThread, newThread);
            }
        }

        public override void ThreadDestroyed(IShadowThread thread, EventInfo info)
        {
            lock (ftLock)
            {
                var originalThread = GetThreadState(thread);
                var newThread = GetThreadState(info.Thread);
                Join(originalThread, newThread);
            }
        }
    }
}
