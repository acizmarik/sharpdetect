﻿using dnlib.DotNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Plugins.LockSet
{
    [PluginExport("Eraser", "1.0.0")]
    [PluginDiagnosticsCategories(new string[] { DiagnosticsCategory })]
    public class EraserPlugin : NopPlugin
    {
        public const string DiagnosticsCategory = "Data-race";
        public const string DiagnosticsMessageFormat = "Affected variable: {0}";

        private readonly ConcurrentDictionary<IShadowObject, ConcurrentDictionary<FieldDef, Variable>> instanceFields;
        private readonly ConcurrentDictionary<IShadowObject, ConcurrentDictionary<int, Variable>> arrayElements;
        private readonly ConcurrentDictionary<FieldDef, Variable> staticFields;
        private readonly ConcurrentDictionary<IShadowThread, HashSet<IShadowObject>> takenLocks;

        private IReportingService reportingService;
        private IMetadataContext metadataContext;
        private IEventDescriptorRegistry eventRegistry;
        private ILogger<EraserPlugin> logger;
        private TypeDef? threadStaticAttribute;

        public EraserPlugin()
        {
            instanceFields = new();
            arrayElements = new();
            staticFields = new();
            takenLocks = new();
            reportingService = null!;
            metadataContext = null!;
            eventRegistry = null!;
            logger = null!;
        }

        public override void Initialize(IServiceProvider serviceProvider)
        {
            reportingService = serviceProvider.GetRequiredService<IReportingService>();
            metadataContext = serviceProvider.GetRequiredService<IMetadataContext>();
            eventRegistry = serviceProvider.GetRequiredService<IEventDescriptorRegistry>();
            logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<EraserPlugin>();
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
            var success = TryAccessArrayElement(info.Thread, instance, index, (takenLocks) =>
            {
                arrayElements.TryGetValue(instance, out var tracked);
                return tracked![index].TryUpdateRead(info.Thread, takenLocks);
            });

            if (!success)
            {
                var sourceLink = eventRegistry.Get(srcMappingId);
                reportingService.CreateReport(
                    plugin: nameof(EraserPlugin),
                    message: string.Format(DiagnosticsMessageFormat, $"{instance}[{index}]"),
                    category: DiagnosticsCategory,
                    processId: info.Runtime.ProcessId,
                    sourceLink);
            }
        }

        public override void ArrayElementWritten(ulong srcMappingId, IShadowObject instance, int index, EventInfo info)
        {
            var success = TryAccessArrayElement(info.Thread, instance, index, (takenLocks) =>
            {
                arrayElements.TryGetValue(instance, out var tracked);
                return tracked![index].TryUpdateWrite(info.Thread, takenLocks);
            });

            if (!success)
            {
                var sourceLink = eventRegistry.Get(srcMappingId);
                reportingService.CreateReport(
                    plugin: nameof(EraserPlugin),
                    message: string.Format(DiagnosticsMessageFormat, $"{instance}[{index}]"),
                    category: DiagnosticsCategory,
                    processId: info.Runtime.ProcessId,
                    sourceLink);
            }
        }

        public override void FieldRead(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info)
        {
            var success = false;
            var sourceLink = eventRegistry.Get(srcMappingId);
            var fieldRef = (IField)sourceLink.Instruction.Operand;
            var resolver = metadataContext.GetResolver(info.Runtime.ProcessId);
            if (!resolver.TryResolveFieldDef(fieldRef, out var fieldDef) || !fieldDef.ShouldAnalyzeForDataRaces(threadStaticAttribute!))
                return;

            if (instance == null)
                success = TryAccessStaticField(info.Thread, fieldDef!, (locks) => staticFields[fieldDef!].TryUpdateRead(info.Thread, locks));
            else
                success = TryAccessInstanceField(info.Thread, fieldDef!, instance, (locks) => instanceFields.TryGetValue(instance, out var tracked)
                    && tracked[fieldDef!].TryUpdateRead(info.Thread, locks));

            if (!success)
            {
                reportingService.CreateReport(
                    plugin: nameof(EraserPlugin),
                    message: string.Format(DiagnosticsMessageFormat, fieldDef),
                    category: DiagnosticsCategory,
                    processId: info.Runtime.ProcessId,
                    sourceLink);
            }
        }

        public override void FieldWritten(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info)
        {
            var success = false;
            var sourceLink = eventRegistry.Get(srcMappingId);
            var fieldRef = (IField)sourceLink.Instruction.Operand;
            var resolver = metadataContext.GetResolver(info.Runtime.ProcessId);
            if (!resolver.TryResolveFieldDef(fieldRef, out var fieldDef) || !fieldDef.ShouldAnalyzeForDataRaces(threadStaticAttribute!))
                return;

            if (instance == null)
                success = TryAccessStaticField(info.Thread, fieldDef!, (locks) => staticFields[fieldDef!].TryUpdateWrite(info.Thread, locks));
            else
                success = TryAccessInstanceField(info.Thread, fieldDef!, instance, (locks) => instanceFields.TryGetValue(instance, out var tracked)
                    && tracked[fieldDef!].TryUpdateWrite(info.Thread, locks));

            if (!success)
            {
                reportingService.CreateReport(
                    plugin: nameof(EraserPlugin),
                    message: string.Format(DiagnosticsMessageFormat, fieldDef),
                    category: DiagnosticsCategory,
                    processId: info.Runtime.ProcessId,
                    sourceLink);
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
