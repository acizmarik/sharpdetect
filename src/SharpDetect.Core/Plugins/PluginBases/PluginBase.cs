// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Core.Events;
using SharpDetect.Core.Loader;
using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Core.Plugins.PluginBases;

public abstract class PluginBase : RecordedEventActionVisitorBase
{
    protected SummaryBuilder Reporter { get; } = new SummaryBuilder();
    protected ILogger Logger { get; }
    protected IModuleBindContext ModuleBindContext { get; }

    protected PluginBase(IModuleBindContext moduleBindContext, ILogger logger)
    {
        ModuleBindContext = moduleBindContext;
        Logger = logger;
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
        Reporter.IncrementMethodEnterExitCounter();
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        Reporter.IncrementMethodEnterExitCounter();
    }

    protected override void DefaultVisit(RecordedEventMetadata metadata, IRecordedEventArgs args)
    {
        /* Ignored event */
    }
}
