using dnlib.DotNet;
using SharpDetect.Common;
using SharpDetect.Common.Diagnostics;
using SharpDetect.Common.Plugins;
using SharpDetect.Common.Plugins.Metadata;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Arguments;
using SharpDetect.Common.Services.Instrumentation;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Common.Services.Reporting;
using SharpDetect.Plugins;

namespace SharpDetect.E2ETests.Utilities
{
    [PluginExport("Reporter", "1.0.0")]
    public class ReporterPlugin : NopPlugin
    {
        private readonly IMetadataContext metadataContext;
        private readonly IReportingService reportingService;
        private readonly IEventDescriptorRegistry eventRegistry;

        public ReporterPlugin(
            IMetadataContext metadataContext,
            IReportingService reportingService,
            IEventDescriptorRegistry eventRegistry)
        {
            this.metadataContext = metadataContext;
            this.reportingService = reportingService;
            this.eventRegistry = eventRegistry;
        }

        public override void AnalysisStarted(EventInfo info) => reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(AnalysisStarted), string.Empty, null, info.Runtime.ProcessId, info.Thread, default));
        public override void AnalysisEnded(EventInfo info) => reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(AnalysisEnded), string.Empty, null, info.Runtime.ProcessId, info.Thread, default));
        public override void MethodCalled(FunctionInfo method, IArgumentsList? arguments, EventInfo info)
        {
            metadataContext.GetResolver(info.Runtime.ProcessId).TryGetMethodDef(method, new(method.ModuleId), resolveWrappers: true, out var methodDef);
            reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(MethodCalled), methodDef?.FullName ?? "<unknown-method>", null, info.Runtime.ProcessId, info.Thread, default));
        }
        public override void MethodReturned(FunctionInfo method, IValueOrObject? returnValue, IArgumentsList? byRefArguments, EventInfo info)
        {
            metadataContext.GetResolver(info.Runtime.ProcessId).TryGetMethodDef(method, new(method.ModuleId), resolveWrappers: true, out var methodDef);
            reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(MethodReturned), methodDef?.FullName ?? "<unknown-method>", null, info.Runtime.ProcessId, info.Thread, default));
        }
        public override void LockAcquireAttempted(IShadowObject instance, EventInfo info) => reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(LockAcquireAttempted), string.Empty, new[] { instance }, info.Runtime.ProcessId, info.Thread, default));
        public override void LockAcquireReturned(IShadowObject instance, bool isSuccess, EventInfo info) => reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(LockAcquireReturned), string.Empty, new[] { instance }, info.Runtime.ProcessId, info.Thread, default));
        public override void LockReleased(IShadowObject instance, EventInfo info) => reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(LockReleased), string.Empty, new[] { instance }, info.Runtime.ProcessId, info.Thread, default));
        public override void FieldRead(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info)
        {
            var mapping = eventRegistry.Get(srcMappingId);
            reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(FieldRead), ((IField)mapping.Instruction.Operand).FullName, new[] { instance }, info.Runtime.ProcessId, info.Thread, default));
        }
        public override void FieldWritten(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info)
        {
            var mapping = eventRegistry.Get(srcMappingId);
            reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(FieldWritten), ((IField)mapping.Instruction.Operand).FullName, new[] { instance }, info.Runtime.ProcessId, info.Thread, default));
        }
        public override void ArrayElementRead(ulong srcMappingId, IShadowObject instance, int index, EventInfo info) => reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(ArrayElementRead), string.Empty, new object[] { instance, index }, info.Runtime.ProcessId, info.Thread, default));
        public override void ArrayElementWritten(ulong srcMappingId, IShadowObject instance, int index, EventInfo info) => reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(ArrayElementWritten), string.Empty, new object[] { instance, index }, info.Runtime.ProcessId, info.Thread, default));
        public override void GarbageCollectionStarted(EventInfo info) => reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(GarbageCollectionStarted), string.Empty, null, info.Runtime.ProcessId, info.Thread, default));
        public override void GarbageCollectionFinished(EventInfo info) => reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(GarbageCollectionFinished), string.Empty, null, info.Runtime.ProcessId, info.Thread, default));
    }
}
