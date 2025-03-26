// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Loader;
using SharpDetect.Core.Plugins.Models;
using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Core.Plugins;

public abstract class PluginBase : RecordedEventActionVisitorBase
{
    protected SummaryBuilder Reporter { get; } = new SummaryBuilder();
    protected ILogger Logger { get; }
    protected IModuleBindContext ModuleBindContext { get; }
    protected IReadOnlyDictionary<ThreadId, string> Threads => _threads;
    protected IReadOnlyDictionary<ThreadId, Callstack> Callstacks => _callstacks;
    private readonly Dictionary<ThreadId, Callstack> _callstacks;
    private readonly Dictionary<ThreadId, string> _threads;
    private int nextFreeThreadId;

    protected PluginBase(IServiceProvider serviceProvider)
    {
        ModuleBindContext = serviceProvider.GetRequiredService<IModuleBindContext>();
        Logger = serviceProvider.GetRequiredService<ILogger<PluginBase>>();
        _callstacks = [];
        _threads = [];
    }

    protected override void Visit(RecordedEventMetadata metadata, ProfilerLoadRecordedEvent args)
    {
        var runtimeInfo = new RuntimeInfo(
            Type: args.RuntimeType,
            Version: new Version(args.MajorVersion, args.MinorVersion, args.BuildVersion, args.QfeVersion));
        Reporter.SetRuntimeInfo(runtimeInfo);
    }

    protected override void Visit(RecordedEventMetadata metadata, ModuleLoadRecordedEvent args)
    {
        var result = ModuleBindContext.TryGetModule(metadata.Pid, args.ModuleId);
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
        var threadId = args.ThreadId;
        if (!Threads.ContainsKey(threadId))
        {
            // Note: if runtime spawns a thread with a custom name, we first receive the rename notification
            StartNewThread(metadata.Pid, args.ThreadId);
        }

        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, ThreadRenameRecordedEvent args)
    {
        var threadId = args.ThreadId;
        if (!Threads.ContainsKey(threadId))
        {
            // Note: if runtime spawns a thread with a custom name, we first receive the rename notification
            StartNewThread(metadata.Pid, args.ThreadId);
        }

        var oldName = Threads[threadId];
        var newName = $"{oldName} ({args.NewName})";
        _threads[threadId] = newName;
        Logger.LogInformation("Renamed thread {OldName} -> {NewName}.", oldName, newName);
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, ThreadDestroyRecordedEvent args)
    {
        var threadId = args.ThreadId;
        var name = Threads[threadId];
        _threads.Remove(threadId);
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
        _callstacks[metadata.Tid].Push(args.ModuleId, args.MethodToken);
        Reporter.IncrementMethodEnterExitCounter();
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        _callstacks[metadata.Tid].Push(args.ModuleId, args.MethodToken);
        Reporter.IncrementMethodEnterExitCounter();
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        _callstacks[metadata.Tid].Pop();
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        _callstacks[metadata.Tid].Pop();
        base.Visit(metadata, args);
    }

    protected override void DefaultVisit(RecordedEventMetadata metadata, IRecordedEventArgs args)
    {
        /* Ignored event */
    }

    private void StartNewThread(uint processId, ThreadId threadId)
    {
        _threads[threadId] = $"T{nextFreeThreadId++}";
        _callstacks[threadId] = new(processId, threadId);
        Logger.LogInformation("Started thread {Name}.", _threads[threadId]);
    }
}
