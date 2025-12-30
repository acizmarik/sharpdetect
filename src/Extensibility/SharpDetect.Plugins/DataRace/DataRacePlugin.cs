// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Configuration;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Loader;
using SharpDetect.Core.Metadata;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Serialization;
using SharpDetect.Plugins.Descriptors;

namespace SharpDetect.Plugins.DataRace;

public partial class DataRacePlugin : HappensBeforeOrderingPluginBase, IPlugin
{
    public string ReportCategory => "DataRace";
    public RecordedEventActionVisitorBase EventsVisitor => this;
    public override PluginConfiguration Configuration { get; }
    public DirectoryInfo ReportTemplates { get; }
    private readonly IMetadataContext _metadataContext;
    
    public DataRacePlugin(
        IModuleBindContext moduleBindContext,
        IMetadataContext metadataContext,
        IArgumentsParser argumentsParser,
        IRecordedEventsDeliveryContext eventsDeliveryContext,
        IProfilerCommandSenderProvider profilerCommandSenderProvider,
        PathsConfiguration pathsConfiguration,
        TimeProvider timeProvider,
        ILogger<DataRacePlugin> logger) 
        : base(
            moduleBindContext,
            metadataContext,
            argumentsParser,
            eventsDeliveryContext,
            profilerCommandSenderProvider,
            timeProvider,
            logger)
    {
        _metadataContext = metadataContext;
        Configuration = PluginConfiguration.Create(
            eventMask: COR_PRF_MONITOR.COR_PRF_MONITOR_ASSEMBLY_LOADS |
                       COR_PRF_MONITOR.COR_PRF_MONITOR_MODULE_LOADS |
                       COR_PRF_MONITOR.COR_PRF_MONITOR_JIT_COMPILATION |
                       COR_PRF_MONITOR.COR_PRF_MONITOR_THREADS |
                       COR_PRF_MONITOR.COR_PRF_MONITOR_ENTERLEAVE |
                       COR_PRF_MONITOR.COR_PRF_MONITOR_GC |
                       COR_PRF_MONITOR.COR_PRF_ENABLE_FUNCTION_ARGS |
                       COR_PRF_MONITOR.COR_PRF_ENABLE_FUNCTION_RETVAL |
                       COR_PRF_MONITOR.COR_PRF_ENABLE_STACK_SNAPSHOT |
                       COR_PRF_MONITOR.COR_PRF_ENABLE_FRAME_INFO |
                       COR_PRF_MONITOR.COR_PRF_DISABLE_INLINING |
                       COR_PRF_MONITOR.COR_PRF_DISABLE_OPTIMIZATIONS |
                       COR_PRF_MONITOR.COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST |
                       COR_PRF_MONITOR.COR_PRF_DISABLE_ALL_NGEN_IMAGES,
            additionalData: new
            {
                MethodDescriptors = MonitorMethodDescriptors.GetAllMethods().Concat(
                    LockMethodDescriptors.GetAllMethods()).Concat(
                    ThreadMethodDescriptors.GetAllMethods()).Concat(
                    FieldAccessDescriptors.GetAllMethods())
                    .ToImmutableArray(),
                TypeInjectionDescriptors = SharpDetectHelperTypeDescriptors.GetAllTypes()
                    .ToImmutableArray(),
                EnableFieldsAccessInstrumentation = true
            },
            temporaryFilesFolder: pathsConfiguration.TemporaryFilesFolder);

        ReportTemplates = new DirectoryInfo(
            Path.Combine(
                Path.GetDirectoryName(GetType().Assembly.Location)!,
                "DataRace",
                "Templates",
                "Partials"));
    }

    protected override void Visit(RecordedEventMetadata metadata, FieldAccessInstrumentationRecordedEvent args)
    {
        var method = _metadataContext.GetResolver(metadata.Pid).ResolveMethod(metadata, args.ModuleId, args.MethodToken).Value;
        Logger.LogInformation("Instrumented field access event in method {Method}", method.FullName);
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, StaticFieldReadRecordedEvent args)
    {
        Logger.LogInformation("Instrumented static field read event: {Event}", args);
        base.Visit(metadata, args);
    }
    
    protected override void Visit(RecordedEventMetadata metadata, StaticFieldWriteRecordedEvent args)
    {
        Logger.LogInformation("Instrumented static field write event: {Event}", args);
        base.Visit(metadata, args);
    }
}