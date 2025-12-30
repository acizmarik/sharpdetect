// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Metadata;

namespace SharpDetect.Metadata.Services;

internal class MetadataEmitter : IMetadataEmitter
{
    public uint ProcessId { get; }
    private readonly InjectedData _state;

    public MetadataEmitter(
        uint processId,
        InjectedData state)
    {
        ProcessId = processId;
        _state = state;
    }

    public void Emit(ModuleId moduleId, MdMethodDef wrapperMethodToken, MdMethodDef wrappedMethodToken)
        => _state.RegisterInjectedMethodWrapper(moduleId, wrapperMethodToken, wrappedMethodToken);
}
