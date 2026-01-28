// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Events;
using SharpDetect.Core.Loader;
using SharpDetect.Core.Metadata;
using SharpDetect.Core.Plugins.Models;
using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Core.Plugins;

public abstract class PluginBase : RecordedEventActionVisitorBase, IDisposable
{
    public abstract PluginConfiguration Configuration { get; }
    protected SummaryBuilder Reporter { get; }
    protected ILogger Logger { get; }
    protected IModuleBindContext ModuleBindContext { get; }
    protected IMetadataContext MetadataContext { get; }
    protected IReadOnlyDictionary<ProcessThreadId, string> Threads { get; }
    protected IReadOnlyDictionary<InstrumentationPointId, InstrumentedFieldAccess> InstrumentedFieldAccesses { get; }
    private readonly Dictionary<ProcessThreadId, Callstack> _callstacks;
    private readonly Dictionary<ProcessThreadId, string> _threads;
    private readonly Dictionary<InstrumentationPointId, InstrumentedFieldAccess> _instrumentedFieldAccesses;
    private readonly IProfilerCommandSenderProvider _profilerCommandSenderProvider;
    private ImmutableDictionary<int, IProfilerCommandSender> _profilerCommandSenders;
    private int _nextFreeThreadId;
    private bool _disposed;

    protected PluginBase(
        IModuleBindContext moduleBindContext,
        IMetadataContext metadataContext,
        IProfilerCommandSenderProvider profilerCommandSenderProvider,
        TimeProvider timeProvider,
        ILogger logger)
    {
        ModuleBindContext = moduleBindContext;
        MetadataContext = metadataContext;
        Reporter = new SummaryBuilder(timeProvider);
        Logger = logger;
        _profilerCommandSenderProvider = profilerCommandSenderProvider;
        _profilerCommandSenders = ImmutableDictionary<int, IProfilerCommandSender>.Empty;
        _callstacks = [];
        _threads = [];
        _instrumentedFieldAccesses = [];

        Threads = _threads;
        InstrumentedFieldAccesses = _instrumentedFieldAccesses;
    }

    protected override void Visit(RecordedEventMetadata metadata, ProfilerLoadRecordedEvent args)
    {
        Reporter.SetStartTime();
        var runtimeInfo = new RuntimeInfo(
            Type: args.RuntimeType,
            Version: new Version(args.MajorVersion, args.MinorVersion, args.BuildVersion, args.QfeVersion));
        Reporter.SetRuntimeInfo(runtimeInfo);
        InitializeCommandsChannel(metadata.Pid);
    }

    protected override void Visit(RecordedEventMetadata metadata, ProfilerInitializeRecordedEvent args)
    {
        Reporter.AddRuntimeProperty("Profiler", "Attached");
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, ProfilerAbortInitializeEvent args)
    {
        Reporter.AddRuntimeProperty("Profiler", $"Aborted. Reason: \"{args.Reason}\"");
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, ModuleLoadRecordedEvent args)
    {
        var result = ModuleBindContext.LoadModule(metadata, args.ModuleId, args.Path);
        if (result.IsError)
            return;

        var moduleDef = result.Value;
        var assemblyDef = moduleDef.Assembly;
        var culture = assemblyDef.Culture.String;
        Reporter.AddModule(new ModuleInfo(
            assemblyDef.Name,
            args.Path,
            assemblyDef.Version,
            string.IsNullOrWhiteSpace(culture) ? "neutral" : culture,
            assemblyDef.PublicKeyToken?.ToString() ?? "null"));
    }

    protected override void Visit(RecordedEventMetadata metadata, ThreadCreateRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        if (!Threads.ContainsKey(id))
        {
            // Note: if runtime spawns a thread with a custom name, we first receive the rename notification
            CreateNewThread(id);
        }

        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, ThreadRenameRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, args.ThreadId);
        if (!Threads.ContainsKey(id))
        {
            // Note: if runtime spawns a thread with a custom name, we first receive the rename notification
            CreateNewThread(id);
        }

        var oldName = Threads[id];
        var newName = $"{oldName} ({args.NewName})";
        _threads[id] = newName;
        Logger.LogInformation("Renamed thread {OldName} -> {NewName}.", oldName, newName);
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, ThreadDestroyRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, args.ThreadId);
        var name = Threads[id];
        _threads.Remove(id);
        Logger.LogInformation("Destroyed thread {Name}.", name);
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, JitCompilationRecordedEvent args)
    {
        Reporter.IncrementAnalyzedMethodsCounter();
    }

    protected override void Visit(RecordedEventMetadata metadata, TypeDefinitionInjectionRecordedEvent args)
    {
        Reporter.IncrementInjectedTypesCounter();
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodWrapperInjectionRecordedEvent args)
    {
        MetadataContext.GetEmitter(metadata.Pid).Emit(args.ModuleId, args.WrapperMethodToken, args.WrappedMethodToken);
        Reporter.IncrementInjectedMethodsCounter();
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodBodyRewriteRecordedEvent args)
    {
        Reporter.IncrementRewrittenMethodsCounter();
    }

    protected override void Visit(RecordedEventMetadata metadata, GarbageCollectionFinishRecordedEvent args)
    {
        Reporter.IncrementGarbageCollectionsCounter();
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodEnterRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        _callstacks[id].Push(args.ModuleId, args.MethodToken);
        Reporter.IncrementMethodEnterExitCounter();
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        _callstacks[id].Push(args.ModuleId, args.MethodToken);
        Reporter.IncrementMethodEnterExitCounter();
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        _callstacks[id].Pop();
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        _callstacks[id].Pop();
        base.Visit(metadata, args);
    }
    
    protected override void Visit(RecordedEventMetadata metadata, FieldAccessInstrumentationRecordedEvent args)
    {
        var instrumentationId = new InstrumentationPointId(metadata.Pid, args.InstrumentationId);
        var instrumentedFieldAccess = new InstrumentedFieldAccess(args.ModuleId, args.MethodToken, args.FieldToken);
        _instrumentedFieldAccesses.Add(instrumentationId, instrumentedFieldAccess);
        base.Visit(metadata, args);
    }

    protected override void DefaultVisit(RecordedEventMetadata metadata, IRecordedEventArgs args)
    {
        /* Ignored event */
    }

    protected IProfilerCommandSender GetCommandSender(uint pid)
    {
        return !_profilerCommandSenders.TryGetValue((int)pid, out var sender) 
            ? throw new InvalidOperationException($"No command sender initialized for process {pid}.")
            : sender;
    }

    private void CreateNewThread(ProcessThreadId processThreadId)
    {
        _threads[processThreadId] = $"T{_nextFreeThreadId++}";
        _callstacks[processThreadId] = new Callstack(processThreadId);
        Logger.LogInformation("Created thread {Name}.", _threads[processThreadId]);
    }

    private void InitializeCommandsChannel(uint processId)
    {
        // Initialize IPC queue for commands sending
        var sender = _profilerCommandSenderProvider.Create(
            ipcQueueName: $"{Configuration.CommandQueueName}.{processId}",
            ipcQueueFileName: Configuration.CommandQueueFile is not null
                ? $"{Configuration.CommandQueueFile}.{processId}"
                : null,
            Configuration.CommandQueueSize);
        _profilerCommandSenders = _profilerCommandSenders.Add((int)processId, sender);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        foreach (var sender in _profilerCommandSenders.Values)
        {
            if (sender is IDisposable disposable)
                disposable.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
