// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Events.Descriptors.Profiler;

namespace SharpDetect.Metadata;

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
