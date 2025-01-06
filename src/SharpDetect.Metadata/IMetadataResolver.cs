﻿// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using OperationResult;
using SharpDetect.Events;
using SharpDetect.Events.Descriptors.Profiler;

namespace SharpDetect.Metadata;

public interface IMetadataResolver
{
    Result<ModuleDef, MetadataResolverErrorType> ResolveModule(RecordedEventMetadata metadata, ModuleId moduleId);
    Result<ModuleDef, MetadataResolverErrorType> ResolveModule(uint pid, ModuleId moduleId);
    Result<TypeDef, MetadataResolverErrorType> ResolveType(RecordedEventMetadata metadata, ModuleId moduleId, MdTypeDef typeToken);
    Result<TypeDef, MetadataResolverErrorType> ResolveType(uint pid, ModuleId moduleId, MdTypeDef typeToken);
    Result<MethodDef, MetadataResolverErrorType> ResolveMethod(RecordedEventMetadata metadata, ModuleId moduleId, MdMethodDef methodToken);
    Result<MethodDef, MetadataResolverErrorType> ResolveMethod(uint pid, ModuleId moduleId, MdMethodDef methodToken);
}
