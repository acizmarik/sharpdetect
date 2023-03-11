// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler;

internal record AssemblyRefProps(
    MdAssemblyRef AssemblyRef,
    string Name,
    IntPtr PublicKey,
    ulong PublicKeyLength,
    ASSEMBLYMETADATA Metadata,
    DWORD Flags);
