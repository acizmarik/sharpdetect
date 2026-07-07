// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Core.Plugins;

public readonly record struct CapturedStackFrame(ModuleId ModuleId, MdMethodDef MethodToken);

public readonly struct CapturedStackTrace(CapturedStackFrame top, byte[]? deeperFramesBlob = null)
{
    // Deeper frames are a flat blob of 12-byte entries: UINT64 moduleId + UINT32 methodToken
    private const int EntrySize = sizeof(ulong) + sizeof(uint);

    public CapturedStackFrame Top { get; } = top;
    
    public IReadOnlyList<CapturedStackFrame> GetDeeperFrames()
    {
        if (deeperFramesBlob is not { Length: > EntrySize })
            return [];

        var count = deeperFramesBlob.Length / EntrySize;
        var frames = new List<CapturedStackFrame>(count - 1);
        var span = deeperFramesBlob.AsSpan();

        // Entry 0 is the accessing method, already exposed as Top.
        for (var i = 1; i < count; i++)
        {
            var offset = i * EntrySize;
            var moduleIdRaw = MemoryMarshal.Read<ulong>(span[offset..]);
            var methodTokenRaw = MemoryMarshal.Read<uint>(span[(offset + sizeof(ulong))..]);
            frames.Add(new CapturedStackFrame(
                new ModuleId((nuint)moduleIdRaw),
                new MdMethodDef((int)methodTokenRaw)));
        }

        return frames;
    }
}
