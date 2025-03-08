// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Metadata;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Plugins.PluginBases;
using SharpDetect.Core.Reporting.Model;
using System.Runtime.InteropServices;

namespace SharpDetect.Plugins.Disposables;

public class DisposablesPlugin : PluginBase, IPlugin
{
    public string ReportCategory => "Disposables";
    public RecordedEventActionVisitorBase EventsVisitor => this;
    public PluginConfiguration Configuration { get; } = PluginConfiguration.Create(
        eventMask: COR_PRF_MONITOR.COR_PRF_MONITOR_MODULE_LOADS |
                   COR_PRF_MONITOR.COR_PRF_MONITOR_ENTERLEAVE |
                   COR_PRF_MONITOR.COR_PRF_MONITOR_GC |
                   COR_PRF_MONITOR.COR_PRF_ENABLE_FUNCTION_ARGS |
                   COR_PRF_MONITOR.COR_PRF_ENABLE_FUNCTION_RETVAL |
                   COR_PRF_MONITOR.COR_PRF_ENABLE_FRAME_INFO |
                   COR_PRF_MONITOR.COR_PRF_DISABLE_INLINING |
                   COR_PRF_MONITOR.COR_PRF_DISABLE_OPTIMIZATIONS |
                   COR_PRF_MONITOR.COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST |
                   COR_PRF_MONITOR.COR_PRF_DISABLE_ALL_NGEN_IMAGES,
        additionalData: null);
    private readonly IMetadataContext _metadataContext;
    private readonly HashSet<TrackedObjectId> _allocated;

    public DisposablesPlugin(
        IMetadataContext metadataContext,
        IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        _metadataContext = metadataContext;
        _allocated = [];
    }

    protected override void Visit(RecordedEventMetadata metadata, ModuleLoadRecordedEvent args)
    {
        ModuleBindContext.LoadModule(metadata, args.ModuleId, args.Path);
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var methodDefResolveResult = _metadataContext
            .GetResolver(metadata.Pid)
            .ResolveMethod(metadata, args.ModuleId, args.MethodToken);

        if (methodDefResolveResult.IsError)
            throw new InvalidOperationException($"Method resolve failed with: {methodDefResolveResult.Error}.");

        var methodDef = methodDefResolveResult.Value;
        var trackedDisposableObjectId = MemoryMarshal.Read<TrackedObjectId>(args.ArgumentValues);

        Logger.LogInformation("Invoked {method} with {objectId}", methodDef.FullName, trackedDisposableObjectId);

        if (methodDef.IsConstructor)
            _allocated.Add(trackedDisposableObjectId);
        else
            _allocated.Remove(trackedDisposableObjectId);
    }

    public Summary CreateDiagnostics()
    {
        throw new NotImplementedException();
    }
}
