// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Metadata;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Reporting.Model;
using SharpDetect.Plugins.Disposables.Descriptors;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace SharpDetect.Plugins.Disposables;

public partial class DisposablesPlugin : PluginBase, IPlugin
{
    public string ReportCategory => "Disposables";
    public RecordedEventActionVisitorBase EventsVisitor => this;
    public PluginConfiguration Configuration { get; } = PluginConfiguration.Create(
        eventMask: COR_PRF_MONITOR.COR_PRF_MONITOR_MODULE_LOADS |
                   COR_PRF_MONITOR.COR_PRF_MONITOR_THREADS |
                   COR_PRF_MONITOR.COR_PRF_MONITOR_ENTERLEAVE |
                   COR_PRF_MONITOR.COR_PRF_MONITOR_GC |
                   COR_PRF_MONITOR.COR_PRF_ENABLE_FUNCTION_ARGS |
                   COR_PRF_MONITOR.COR_PRF_ENABLE_FUNCTION_RETVAL |
                   COR_PRF_MONITOR.COR_PRF_ENABLE_FRAME_INFO |
                   COR_PRF_MONITOR.COR_PRF_DISABLE_INLINING |
                   COR_PRF_MONITOR.COR_PRF_DISABLE_OPTIMIZATIONS |
                   COR_PRF_MONITOR.COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST |
                   COR_PRF_MONITOR.COR_PRF_DISABLE_ALL_NGEN_IMAGES,
        additionalData: new
        {
            TypesToIgnore = DisposablesDescriptors.GetAllTypeIgnores().ToImmutableArray(),
            MethodsToInclude = DisposablesDescriptors.GetAllMethodDescriptors().ToImmutableArray()
        });
    public DirectoryInfo ReportTemplates { get; }

    private readonly IMetadataContext _metadataContext;
    private readonly ICallstackResolver _callstackResolver;
    private readonly HashSet<TrackedObjectId> _notDisposed;
    private readonly Dictionary<TrackedObjectId, AllocationInfo> _allocationInfos;

    public DisposablesPlugin(
        IMetadataContext metadataContext,
        ICallstackResolver callstackResolver,
        IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        _metadataContext = metadataContext;
        _callstackResolver = callstackResolver;
        _notDisposed = [];
        _allocationInfos = [];

        ReportTemplates = new DirectoryInfo(
            Path.Combine(
                Path.GetDirectoryName(GetType().Assembly.Location)!,
                "Disposables",
                "Templates",
                "Partials"));
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

        var threadInfo = new ThreadInfo(metadata.Tid.Value, Threads[metadata.Tid]);
        Logger.LogInformation("Invoked {method} with {objectId}", methodDef.FullName, trackedDisposableObjectId);

        if (methodDef.IsConstructor)
        {
            var callstackCopy = Callstacks[metadata.Tid].Clone();
            callstackCopy.Push(args.ModuleId, args.MethodToken);
            var allocationInfo = new AllocationInfo(methodDef, metadata.Pid, threadInfo, callstackCopy);
            if (_notDisposed.Add(trackedDisposableObjectId))
                _allocationInfos.Add(trackedDisposableObjectId, allocationInfo);
        }
        else
        {
            _notDisposed.Remove(trackedDisposableObjectId);
            _allocationInfos.Remove(trackedDisposableObjectId);
        }

        base.Visit(metadata, args);
    }
}
