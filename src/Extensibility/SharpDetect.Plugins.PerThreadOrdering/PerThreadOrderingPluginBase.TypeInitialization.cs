// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.PerThreadOrdering;

public abstract partial class PerThreadOrderingPluginBase
{
    public event Action<TypeInitializerArgs>? TypeInitializerEntered;
    public event Action<TypeInitializerArgs>? TypeInitialized;
    public event Action<TypeInitializerArgs>? TypeInitializerFailed;

    private void RegisterTypeInitializationBindings()
    {
        Bind<MethodEnterRecordedEvent>(RecordedEventType.TypeInitializerEnter, OnTypeInitializerEnter);
        Bind<MethodExitRecordedEvent>(RecordedEventType.TypeInitializerExit, OnTypeInitializerExit);
    }

    private void OnTypeInitializerEnter(RecordedEventMetadata metadata, MethodEnterRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        TypeInitializerEntered?.Invoke(new TypeInitializerArgs(id, args.ModuleId, args.MethodToken));
    }

    private void OnTypeInitializerExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        TypeInitialized?.Invoke(new TypeInitializerArgs(id, args.ModuleId, args.MethodToken));
    }

    private void OnTypeInitializerUnwound(ProcessThreadId id, MethodUnwoundRecordedEvent args)
        => TypeInitializerFailed?.Invoke(new TypeInitializerArgs(id, args.ModuleId, args.MethodToken));
}
