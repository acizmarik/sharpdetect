// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler;

[Flags]
public enum CorOpenFlags : uint
{
    ofRead = 0x00000000,
    ofWrite = 0x00000001,
    ofReadWriteMask = 0x00000001,
    ofCopyMemory = 0x00000002,
    ofCacheImage = 0x00000004,
    ofManifestMetadata = 0x00000008,
    ofReadOnly = 0x00000010,
    ofTakeOwnership = 0x00000020,
    ofNoTypeLib = 0x00000080,
    ofNoTransform = 0x00001000,
    ofReserved1 = 0x00000100,
    ofReserved2 = 0x00000200,
    ofReserved = 0xffffff40
}
