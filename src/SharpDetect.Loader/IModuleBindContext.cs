// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using dnlib.DotNet;
using OperationResult;
using SharpDetect.Events;
using SharpDetect.Events.Descriptors.Profiler;

namespace SharpDetect.Loaders;

public interface IModuleBindContext
{
    Result<ModuleDef, ModuleLoadErrorType> LoadModule(RecordedEventMetadata metadata, ModuleId moduleId, string path);
    Result<ModuleDef, ModuleLoadErrorType> LoadModule(RecordedEventMetadata metadata, ModuleId moduleId, Stream stream, string virtualPath);
    Result<ModuleDef, ModuleLoadErrorType> TryGetModule(RecordedEventMetadata metadata, ModuleId moduleId);
    Result<ModuleDef, ModuleLoadErrorType> TryGetModule(uint pid, ModuleId moduleId);
}
