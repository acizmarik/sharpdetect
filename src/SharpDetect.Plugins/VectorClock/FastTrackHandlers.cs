using dnlib.DotNet;
using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Common;
using SharpDetect.Common.Diagnostics;
using SharpDetect.Common.Plugins;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Threads;
using SharpDetect.Common.Services.Instrumentation;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Common.Services.Reporting;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

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

        private IReportingService reportingService;
        private IMetadataContext metadataContext;
        private IEventDescriptorRegistry eventRegistry;
        private TypeDef? threadStaticAttribute;

        public FastTrackPlugin()
        {
            this.threads = new();
            this.locks = new();
            this.instanceFields = new();
            this.arrayElements = new();
            this.staticFields = new();
            this.ftLock = new();

            this.reportingService = null!;
            this.metadataContext = null!;
            this.eventRegistry = null!;
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

        public override void Initialize(IServiceProvider serviceProvider)
        {
            reportingService = serviceProvider.GetRequiredService<IReportingService>();
            metadataContext = serviceProvider.GetRequiredService<IMetadataContext>();
            eventRegistry = serviceProvider.GetRequiredService<IEventDescriptorRegistry>();
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
                    AddViolation(instance, index, info);
            }
        }

        public override void ArrayElementWritten(ulong srcMappingId, IShadowObject instance, int index, EventInfo info)
        {
            lock (ftLock)
            {
                var thread = GetThreadState(info.Thread);
                var variable = GetArrayElementVariableState(thread, true, instance, index);
                if (!Write(variable, thread))
                    AddViolation(instance, index, info);
            }
        }

        public override void FieldRead(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info)
        {
            var fieldRef = (IField)eventRegistry.Get(srcMappingId).Instruction.Operand;
            if (!ShouldAnalyzeField(fieldRef, info.Runtime.ProcessId, out var fieldDef))
                return;

            lock (ftLock)
            {
                var thread = GetThreadState(info.Thread);
                var variable = GetFieldVariableState(thread, false, fieldDef, instance);
                if (!Read(variable, thread))
                     AddViolation(fieldDef, info);
            }
        }

        public override void FieldWritten(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info)
        {
            var fieldRef = (IField)eventRegistry.Get(srcMappingId).Instruction.Operand;
            if (!ShouldAnalyzeField(fieldRef, info.Runtime.ProcessId, out var fieldDef))
                return;

            lock (ftLock)
            {
                var thread = GetThreadState(info.Thread);
                var variable = GetFieldVariableState(thread, true, fieldDef, instance);
                if (!Write(variable, thread))
                    AddViolation(fieldDef, info);
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

        private void AddViolation(FieldDef fieldDef, EventInfo info)
        {
            reportingService.Report(
                new ErrorReport(
                    nameof(FastTrackPlugin),
                        DiagnosticsCategory,
                        string.Format(DiagnosticsMessageFormat, fieldDef),
                        info.Runtime.ProcessId,
                        null));
        }

        private void AddViolation(IShadowObject array, int index, EventInfo info)
        {
            reportingService.Report(
                new ErrorReport(
                    nameof(FastTrackPlugin),
                        DiagnosticsCategory,
                        string.Format(DiagnosticsMessageFormat, $"{array}[{index}]"),
                        info.Runtime.ProcessId,
                        null));
        }

        private bool ShouldAnalyzeField(IField fieldRef, int pid, [NotNullWhen(returnValue: true)] out FieldDef? fieldDef)
        {
            // Do not proceed if we can not resolve the field
            var resolver = metadataContext.GetResolver(pid);
            if (!resolver.TryResolveFieldDef(fieldRef, out fieldDef))
                return false;

            // Readonly fields can not be involved in a data-race
            if (fieldDef.IsInitOnly)
                return false;

            // ThreadStatic annotated fields can not be involved in a data-race
            if (fieldDef.HasCustomAttributes && fieldDef.CustomAttributes.FirstOrDefault(a => a.AttributeType == threadStaticAttribute) != null)
                return false;

            return true;
        }
    }
}
