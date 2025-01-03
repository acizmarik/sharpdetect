// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Events.Descriptors.Profiler;

namespace SharpDetect.Metadata;

public interface IMetadataEmitter
{
    uint ProcessId { get; }

    /// <summary>
    /// Emits a new method into the specified module
    /// </summary>
    void Emit(ModuleId moduleId, MdMethodDef wrapperMethodToken, MdMethodDef wrappedMethodToken);
}
