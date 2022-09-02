using dnlib.DotNet;
using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Common;
using SharpDetect.Common.Diagnostics;
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
        private IMetadataContext metadataContext;
        private IReportingService reportingService;
        private IEventDescriptorRegistry eventRegistry;

        public ReporterPlugin()
        {
            metadataContext = null!;
            reportingService = null!;
            eventRegistry = null!;
        }

        public override void Initialize(IServiceProvider serviceProvider)
        {
            metadataContext = serviceProvider.GetRequiredService<IMetadataContext>();
            reportingService = serviceProvider.GetRequiredService<IReportingService>();
            eventRegistry = serviceProvider.GetRequiredService<IEventDescriptorRegistry>();
        }

        public override void AnalysisStarted(EventInfo info) => reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(AnalysisStarted), string.Empty, info.ProcessId, default));
        public override void AnalysisEnded(EventInfo info) => reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(AnalysisEnded), string.Empty, info.ProcessId, default));
        public override void MethodCalled(FunctionInfo method, IArgumentsList? arguments, EventInfo info)
        {
            metadataContext.GetResolver(info.ProcessId).TryGetMethodDef(method, new(method.ModuleId), resolveWrappers: true, out var methodDef);
            reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(MethodCalled), methodDef?.FullName ?? "<unknown-method>", info.ProcessId, default));
        }
        public override void MethodReturned(FunctionInfo method, IValueOrObject? returnValue, IArgumentsList? byRefArguments, EventInfo info)
        {
            metadataContext.GetResolver(info.ProcessId).TryGetMethodDef(method, new(method.ModuleId), resolveWrappers: true, out var methodDef);
            reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(MethodReturned), methodDef?.FullName ?? "<unknown-method>", info.ProcessId, default));
        }
        public override void LockAcquireAttempted(IShadowObject instance, EventInfo info) => reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(LockAcquireAttempted), string.Empty, info.ProcessId, default));
        public override void LockAcquireReturned(IShadowObject instance, bool isSuccess, EventInfo info) => reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(LockAcquireReturned), string.Empty, info.ProcessId, default));
        public override void LockReleased(IShadowObject instance, EventInfo info) => reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(LockReleased), string.Empty, info.ProcessId, default));
        public override void FieldRead(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info)
        {
            var mapping = eventRegistry.Get(srcMappingId);
            reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(FieldRead), ((IField)mapping.Instruction.Operand).FullName, info.ProcessId, default));
        }
        public override void FieldWritten(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info)
        {
            var mapping = eventRegistry.Get(srcMappingId);
            reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(FieldWritten), ((IField)mapping.Instruction.Operand).FullName, info.ProcessId, default));
        }
        public override void ArrayElementRead(ulong srcMappingId, IShadowObject instance, int index, EventInfo info) => reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(ArrayElementRead), string.Empty, info.ProcessId, default));
        public override void ArrayElementWritten(ulong srcMappingId, IShadowObject instance, int index, EventInfo info) => reportingService.Report(new InformationReport(nameof(ReporterPlugin), nameof(ArrayElementWritten), string.Empty, info.ProcessId, default));
    }
}
