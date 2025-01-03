// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Events.Descriptors.Profiler;
using System.Collections.Immutable;

namespace SharpDetect.Metadata;

internal sealed class InjectedData
{
    public readonly uint ProcessId;
    private ImmutableDictionary<(ModuleId, MdMethodDef), MdMethodDef> _injectedWrappers;

    public InjectedData(uint processId)
    {
        ProcessId = processId;
        _injectedWrappers = ImmutableDictionary<(ModuleId, MdMethodDef), MdMethodDef>.Empty;
    }

    public void RegisterInjectedMethodWrapper(ModuleId moduleId, MdMethodDef wrapperMethodToken, MdMethodDef wrappedMethodToken)
        => _injectedWrappers = _injectedWrappers.Add((moduleId, wrapperMethodToken), wrappedMethodToken);

    public bool IsInjectedWrapperMethod(ModuleId moduleId, MdMethodDef wrapperMethodToken)
        => _injectedWrappers.ContainsKey((moduleId, wrapperMethodToken));

    public bool TryGetWrappedMethod(ModuleId moduleId, MdMethodDef wrapperMethodToken, out MdMethodDef wrappedMethodToken)
        => _injectedWrappers.TryGetValue((moduleId, wrapperMethodToken), out wrappedMethodToken);
}
