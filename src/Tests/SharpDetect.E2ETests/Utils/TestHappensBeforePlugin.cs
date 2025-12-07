// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using dnlib.DotNet;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Metadata;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Reporting.Model;
using SharpDetect.Plugins;
using SharpDetect.Plugins.Descriptors;

namespace SharpDetect.E2ETests.Utils;

public sealed class TestHappensBeforePlugin : HappensBeforeOrderingPluginBase, IPlugin
{
    public string ReportCategory => "Test";
    public RecordedEventActionVisitorBase EventsVisitor => this;
    public override PluginConfiguration Configuration { get; } = PluginConfiguration.Create(
        eventMask: COR_PRF_MONITOR.COR_PRF_MONITOR_ASSEMBLY_LOADS |
                   COR_PRF_MONITOR.COR_PRF_MONITOR_MODULE_LOADS |
                   COR_PRF_MONITOR.COR_PRF_MONITOR_JIT_COMPILATION |
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
        additionalData: MonitorMethodDescriptors.GetAllMethods()
            .Concat(ThreadMethodDescriptors.GetAllMethods())
            .Concat(TestMethodDescriptors.GetAllTestMethods())
            .ToImmutableArray());
    public DirectoryInfo ReportTemplates => throw new NotSupportedException();
    public Summary CreateDiagnostics() => throw new NotSupportedException();
    public IEnumerable<object> CreateReportDataContext(IEnumerable<Report> reports) => throw new NotSupportedException();

    public event Action<(RecordedEventMetadata Metadata, AssemblyLoadRecordedEvent Args)>? AssemblyLoaded;
    public event Action<(RecordedEventMetadata Metadata, AssemblyReferenceInjectionRecordedEvent Args)>? AssemblyReferenceInjected;
    public event Action<(RecordedEventMetadata Metadata, GarbageCollectionStartRecordedEvent Args)>? GarbageCollectionStarted;
    public event Action<(RecordedEventMetadata Metadata, GarbageCollectedTrackedObjectsRecordedEvent Args)>? GarbageCollectedTrackedObjects;
    public event Action<(RecordedEventMetadata Metadata, GarbageCollectionFinishRecordedEvent Args)>? GarbageCollectionFinished;
    public event Action<(RecordedEventMetadata Metadata, JitCompilationRecordedEvent Args)>? JitCompilationStarted;
    public event Action<(RecordedEventMetadata Metadata, MethodBodyRewriteRecordedEvent Args)>? MethodBodyRewritten;
    public event Action<(RecordedEventMetadata Metadata, MethodDefinitionInjectionRecordedEvent Args)>? MethodDefinitionInjected;
    public event Action<(RecordedEventMetadata Metadata, MethodEnterRecordedEvent Args)>? MethodEntered;
    public event Action<(RecordedEventMetadata Metadata, MethodEnterWithArgumentsRecordedEvent Args)>? MethodEnteredWithArguments;
    public event Action<(RecordedEventMetadata Metadata, MethodExitRecordedEvent Args)>? MethodExited;
    public event Action<(RecordedEventMetadata Metadata, MethodExitWithArgumentsRecordedEvent Args)>? MethodExitedWithArguments;
    public event Action<(RecordedEventMetadata Metadata, MethodReferenceInjectionRecordedEvent Args)>? MethodReferenceInjected;
    public event Action<(RecordedEventMetadata Metadata, MethodWrapperInjectionRecordedEvent Args)>? MethodWrapperInjected;
    public event Action<(RecordedEventMetadata Metadata, ModuleLoadRecordedEvent Args)>? ModuleLoaded;
    public event Action<(RecordedEventMetadata Metadata, ProfilerDestroyRecordedEvent Args)>? ProfilerDestroyed;
    public event Action<(RecordedEventMetadata Metadata, ProfilerInitializeRecordedEvent Args)>? ProfilerInitialized;
    public event Action<(RecordedEventMetadata Metadata, ProfilerLoadRecordedEvent Args)>? ProfilerLoaded;
    public event Action<(RecordedEventMetadata Metadata, TailcallRecordedEvent Args)>? Tailcalled;
    public event Action<(RecordedEventMetadata Metadata, TailcallWithArgumentsRecordedEvent Args)>? TailcalledWithArguments;
    public event Action<(RecordedEventMetadata Metadata, ThreadCreateRecordedEvent Args)>? ThreadCreated;
    public event Action<(RecordedEventMetadata Metadata, ThreadDestroyRecordedEvent Args)>? ThreadDestroyed;
    public event Action<(RecordedEventMetadata Metadata, ThreadRenameRecordedEvent Args)>? ThreadRenamed;
    public event Action<(RecordedEventMetadata Metadata, TypeDefinitionInjectionRecordedEvent Args)>? TypeDefinitionInjected;
    public event Action<(RecordedEventMetadata Metadata, TypeLoadRecordedEvent Args)>? TypeLoaded;
    public event Action<(RecordedEventMetadata Metadata, TypeReferenceInjectionRecordedEvent Args)>? TypeReferenceInjected;
    private readonly IMetadataContext _metadataContext;

    public TestHappensBeforePlugin(
        IMetadataContext metadataContext,
        IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        _metadataContext = metadataContext;
    }

    public MethodDef Resolve(RecordedEventMetadata metadata, ModuleId moduleId, MdMethodDef methodToken)
    {
        return _metadataContext
            .GetResolver(metadata.Pid)
            .ResolveMethod(metadata, moduleId, methodToken)
            .Value;
    }

    public string GetThreadName(ProcessThreadId processThreadId)
    {
        return Threads[processThreadId];
    }

    protected override void Visit(RecordedEventMetadata metadata, AssemblyLoadRecordedEvent args)
    {
        base.Visit(metadata, args);
        AssemblyLoaded?.Invoke((metadata, args));
    }

    protected override void Visit(RecordedEventMetadata metadata, AssemblyReferenceInjectionRecordedEvent args)
    {
        base.Visit(metadata, args);
        AssemblyReferenceInjected?.Invoke((metadata, args));
    }

    protected override void Visit(RecordedEventMetadata metadata, GarbageCollectionFinishRecordedEvent args)
    {
        base.Visit(metadata, args);
        GarbageCollectionFinished?.Invoke((metadata, args));
    }

    protected override void Visit(RecordedEventMetadata metadata, GarbageCollectedTrackedObjectsRecordedEvent args)
    {
        base.Visit(metadata, args);
        GarbageCollectedTrackedObjects?.Invoke((metadata, args));
    }

    protected override void Visit(RecordedEventMetadata metadata, GarbageCollectionStartRecordedEvent args)
    {
        base.Visit(metadata, args);
        GarbageCollectionStarted?.Invoke((metadata, args));
    }

    protected override void Visit(RecordedEventMetadata metadata, JitCompilationRecordedEvent args)
    {
        base.Visit(metadata, args);
        JitCompilationStarted?.Invoke((metadata, args));
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodBodyRewriteRecordedEvent args)
    {
        base.Visit(metadata, args);
        MethodBodyRewritten?.Invoke((metadata, args));
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodDefinitionInjectionRecordedEvent args)
    {
        base.Visit(metadata, args);
        MethodDefinitionInjected?.Invoke((metadata, args));
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodEnterRecordedEvent args)
    {
        base.Visit(metadata, args);
        MethodEntered?.Invoke((metadata, args));
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        base.Visit(metadata, args);
        MethodEnteredWithArguments?.Invoke((metadata, args));
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        base.Visit(metadata, args);
        MethodExited?.Invoke((metadata, args));
    }
    protected override void Visit(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        base.Visit(metadata, args);
        MethodExitedWithArguments?.Invoke((metadata, args));
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodReferenceInjectionRecordedEvent args)
    {
        base.Visit(metadata, args);
        MethodReferenceInjected?.Invoke((metadata, args));
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodWrapperInjectionRecordedEvent args)
    {
        base.Visit(metadata, args);
        MethodWrapperInjected?.Invoke((metadata, args));
    }
    protected override void Visit(RecordedEventMetadata metadata, ModuleLoadRecordedEvent args)
    {
        base.Visit(metadata, args);
        ModuleLoaded?.Invoke((metadata, args));
    }
    protected override void Visit(RecordedEventMetadata metadata, ProfilerDestroyRecordedEvent args)
    {
        base.Visit(metadata, args);
        ProfilerDestroyed?.Invoke((metadata, args));
    }

    protected override void Visit(RecordedEventMetadata metadata, ProfilerInitializeRecordedEvent args)
    {
        base.Visit(metadata, args);
        ProfilerInitialized?.Invoke((metadata, args));
    }

    protected override void Visit(RecordedEventMetadata metadata, ProfilerLoadRecordedEvent args)
    {
        base.Visit(metadata, args);
        ProfilerLoaded?.Invoke((metadata, args));
    }

    protected override void Visit(RecordedEventMetadata metadata, TailcallRecordedEvent args)
    {
        base.Visit(metadata, args);
        Tailcalled?.Invoke((metadata, args));
    }

    protected override void Visit(RecordedEventMetadata metadata, TailcallWithArgumentsRecordedEvent args)
    {
        base.Visit(metadata, args);
        TailcalledWithArguments?.Invoke((metadata, args));
    }

    protected override void Visit(RecordedEventMetadata metadata, ThreadCreateRecordedEvent args)
    {
        base.Visit(metadata, args);
        ThreadCreated?.Invoke((metadata, args));
    }

    protected override void Visit(RecordedEventMetadata metadata, ThreadDestroyRecordedEvent args)
    {
        base.Visit(metadata, args);
        ThreadDestroyed?.Invoke((metadata, args));
    }

    protected override void Visit(RecordedEventMetadata metadata, ThreadRenameRecordedEvent args)
    {
        base.Visit(metadata, args);
        ThreadRenamed?.Invoke((metadata, args));
    }

    protected override void Visit(RecordedEventMetadata metadata, TypeDefinitionInjectionRecordedEvent args)
    {
        base.Visit(metadata, args);
        TypeDefinitionInjected?.Invoke((metadata, args));
    }

    protected override void Visit(RecordedEventMetadata metadata, TypeLoadRecordedEvent args)
    {
        base.Visit(metadata, args);
        TypeLoaded?.Invoke((metadata, args));
    }

    protected override void Visit(RecordedEventMetadata metadata, TypeReferenceInjectionRecordedEvent args)
    {
        base.Visit(metadata, args);
        TypeReferenceInjected?.Invoke((metadata, args));
    }
}
