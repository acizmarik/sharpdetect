// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;

namespace SharpDetect.Core.Metadata;

public interface IMetadataResolversProvider
{
    AssemblyResolver AssemblyResolver { get; }
    Resolver MemberResolver { get; }
}
