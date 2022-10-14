using dnlib.DotNet;
using SharpDetect.Common;
using SharpDetect.Common.Diagnostics;
using SharpDetect.Common.Plugins;
using SharpDetect.Common.Plugins.Metadata;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Threads;
using SharpDetect.Common.Services.Instrumentation;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Common.Services.Reporting;
using SharpDetect.Common.SourceLinks;
using SharpDetect.Plugins.Utilities;
using System;
using System.Collections.Concurrent;

namespace SharpDetect.Plugins.LockSet
{
    [PluginExport("Eraser", "1.0.0")]
    [PluginDiagnosticsCategories(new string[] { DiagnosticsCategory })]
    public class EraserPlugin : NopPlugin
    {
        public const string DiagnosticsCategory = "Data-race";
        public const string DiagnosticsMessageFormatArrays = "Possible data-race on array element: {1}[{2}]";
        public const string DiagnosticsMessageFormatFields = "Possible data-race on field: {1}";

        private readonly ConcurrentDictionary<IShadowObject, ConcurrentDictionary<FieldDef, Variable>> instanceFields;
        private readonly ConcurrentDictionary<IShadowObject, ConcurrentDictionary<int, Variable>> arrayElements;
        private readonly ConcurrentDictionary<FieldDef, Variable> staticFields;
        private readonly ConcurrentDictionary<IShadowThread, HashSet<IShadowObject>> takenLocks;
        private readonly MemoryAccessRegistry memoryAccesses;

        private readonly IMetadataContext metadataContext;
        private readonly IReportingService reportingService;
        private readonly IEventDescriptorRegistry eventRegistry;
        private TypeDef? threadStaticAttribute;

        public EraserPlugin(
            IMetadataContext metadataContext, 
            IReportingService reportingService, 
            IEventDescriptorRegistry eventRegistry)
        {
            this.metadataContext = metadataContext;
            this.reportingService = reportingService;
            this.eventRegistry = eventRegistry;

            memoryAccesses = new();
            instanceFields = new();
            arrayElements = new();
            staticFields = new();
            takenLocks = new();
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
            var sourceLink = eventRegistry.Get(srcMappingId);
            var result = TryAccessArrayElement(info.Thread, instance, index, (takenLocks) =>
            {
                memoryAccesses.RegisterAccess(instance, index, new ReportDataEntry(
                    info.Runtime.ProcessId, info.Thread, AnalysisEventType.ArrayElementRead, sourceLink));
                arrayElements.TryGetValue(instance, out var tracked);
                return tracked![index].TryUpdateRead(info.Thread, takenLocks);
            });

            if (!result)
            {
                reportingService.CreateReport(
                    plugin: nameof(EraserPlugin),
                    messageFormat: DiagnosticsMessageFormatArrays,
                    arguments: new object[] { instance, index },
                    category: DiagnosticsCategory,
                    entries: memoryAccesses.GetAllAccesses(instance, index).ToArray());
            }
        }

        public override void ArrayElementWritten(ulong srcMappingId, IShadowObject instance, int index, EventInfo info)
        {
            var sourceLink = eventRegistry.Get(srcMappingId);
            var result = TryAccessArrayElement(info.Thread, instance, index, (takenLocks) =>
            {
                memoryAccesses.RegisterAccess(instance, index, new ReportDataEntry(
                    info.Runtime.ProcessId, info.Thread, AnalysisEventType.ArrayElementWrite, sourceLink));
                arrayElements.TryGetValue(instance, out var tracked);
                return tracked![index].TryUpdateWrite(info.Thread, takenLocks);
            });

            if (!result)
            {
                reportingService.CreateReport(
                    plugin: nameof(EraserPlugin),
                    messageFormat: DiagnosticsMessageFormatArrays,
                    arguments: new object[] { instance, index },
                    category: DiagnosticsCategory,
                    entries: memoryAccesses.GetAllAccesses(instance, index).ToArray());
            }
        }

        public override void FieldRead(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info)
        {
            var result = false;
            var sourceLink = eventRegistry.Get(srcMappingId);
            var fieldRef = (IField)sourceLink.Instruction.Operand;
            var resolver = metadataContext.GetResolver(info.Runtime.ProcessId);
            if (!resolver.TryResolveFieldDef(fieldRef, out var fieldDef) || !fieldDef.ShouldAnalyzeForDataRaces(threadStaticAttribute!))
                return;

            memoryAccesses.RegisterAccess(fieldDef.MDToken, new ReportDataEntry(
                info.Runtime.ProcessId, info.Thread, AnalysisEventType.FieldRead, sourceLink));

            if (instance == null)
                result = TryAccessStaticField(info.Thread, fieldDef!, (locks) => staticFields[fieldDef!].TryUpdateRead(info.Thread, locks));
            else
                result = TryAccessInstanceField(info.Thread, fieldDef!, instance, (locks) => instanceFields.TryGetValue(instance, out var tracked)
                    && tracked[fieldDef!].TryUpdateRead(info.Thread, locks));

            if (!result)
            {
                reportingService.CreateReport(
                    plugin: nameof(EraserPlugin),
                    messageFormat: DiagnosticsMessageFormatFields,
                    arguments: new[] { fieldDef },
                    category: DiagnosticsCategory,
                    entries: memoryAccesses.GetAllAccesses(fieldDef.MDToken).ToArray());
            }
        }

        public override void FieldWritten(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info)
        {
            var result = false;
            var sourceLink = eventRegistry.Get(srcMappingId);
            var fieldRef = (IField)sourceLink.Instruction.Operand;
            var resolver = metadataContext.GetResolver(info.Runtime.ProcessId);
            if (!resolver.TryResolveFieldDef(fieldRef, out var fieldDef) || !fieldDef.ShouldAnalyzeForDataRaces(threadStaticAttribute!))
                return;

            memoryAccesses.RegisterAccess(fieldDef.MDToken, new ReportDataEntry(
                info.Runtime.ProcessId, info.Thread, AnalysisEventType.FieldWrite, sourceLink));

            if (instance == null)
                result = TryAccessStaticField(info.Thread, fieldDef!, (locks) => staticFields[fieldDef!].TryUpdateWrite(info.Thread, locks));
            else
                result = TryAccessInstanceField(info.Thread, fieldDef!, instance, (locks) => instanceFields.TryGetValue(instance, out var tracked)
                    && tracked[fieldDef!].TryUpdateWrite(info.Thread, locks));

            if (!result)
            {
                reportingService.CreateReport(
                    plugin: nameof(EraserPlugin),
                    messageFormat: DiagnosticsMessageFormatFields,
                    arguments: new[] { fieldDef },
                    category: DiagnosticsCategory,
                    entries: memoryAccesses.GetAllAccesses(fieldDef.MDToken).ToArray());
            }
        }

        public override void LockAcquireReturned(IShadowObject instance, bool isSuccess, EventInfo info)
        {
            if (!isSuccess)
                return;

            var set = takenLocks[info.Thread];
            lock (set)
                set.Add(instance);
        }

        public override void LockReleased(IShadowObject instance, EventInfo info)
        {
            var set = takenLocks[info.Thread];
            lock (set)
                set.Remove(instance);
        }

        public override void ThreadCreated(IShadowThread thread, EventInfo info)
        {
            takenLocks.TryAdd(thread, new());
        }

        public override void ThreadDestroyed(IShadowThread thread, EventInfo info)
        {
            takenLocks.Remove(thread, out var _);
        }

        private bool TryAccessStaticField(IShadowThread thread, FieldDef field, Func<HashSet<IShadowObject>, bool> validator)
        {
            lock (field)
            {
                // Make sure we are tracking current field
                staticFields.TryGetValue(field, out var tracked);
                var threadLocks = takenLocks[thread];
                lock (threadLocks)
                {
                    if (tracked == null)
                    {
                        lock (threadLocks)
                            tracked = new Variable(thread, threadLocks);
                        staticFields.TryAdd(field, tracked);
                    }

                    // Update access information
                    return validator(threadLocks);
                }
            }
        }

        private bool TryAccessInstanceField(IShadowThread thread, FieldDef field, IShadowObject instance, Func<HashSet<IShadowObject>, bool> validator)
        {
            lock (instance)
            {
                // Make sure we are tracking current instance
                instanceFields.TryGetValue(instance, out var tracked);
                if (tracked == null)
                {
                    tracked = new();
                    instanceFields.TryAdd(instance, tracked);
                }

                // Make sure we are tracking current field
                var threadLocks = takenLocks[thread];
                lock (threadLocks)
                {
                    if (!tracked.ContainsKey(field))
                        tracked.TryAdd(field, new Variable(thread, threadLocks));

                    // Update access information
                    return validator(threadLocks);
                }
            }
        }


        private bool TryAccessArrayElement(IShadowThread thread, IShadowObject array, int index, Func<HashSet<IShadowObject>, bool> validator)
        {
            lock (array)
            {
                // Make sure we are tracking current array
                arrayElements.TryGetValue(array, out var tracked);
                if (tracked == null)
                {
                    tracked = new();
                    arrayElements.TryAdd(array, tracked);
                }

                // Make sure we are tracking current array element
                arrayElements.TryGetValue(array, out var trackedArray);
                var threadLocks = takenLocks[thread];
                lock (threadLocks)
                {
                    if (!trackedArray!.ContainsKey(index))
                        trackedArray.TryAdd(index, new Variable(thread, threadLocks));

                    // Update access information
                    return validator(threadLocks);
                }
            }
        }
    }
}
