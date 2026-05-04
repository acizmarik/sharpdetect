// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using OperationResult;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Metadata;

namespace SharpDetect.E2ETests.Utils;

public interface IMetadataResolver
{
    Result<MethodDef, MetadataResolverErrorType> Resolve(RecordedEventMetadata metadata, ModuleId moduleId, MdMethodDef methodToken);
}
