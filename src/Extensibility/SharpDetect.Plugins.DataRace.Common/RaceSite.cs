// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.Common;

public readonly record struct RaceSite(
    nuint ModuleId,
    int MethodToken,
    uint MethodOffset,
    AccessType AccessType,
    int CallPathHash) : IComparable<RaceSite>
{
    public static RaceSite From(AccessInfo access) => new(
        access.Stack.Top.ModuleId.Value,
        access.Stack.Top.MethodToken.Value,
        access.MethodOffset,
        access.AccessType,
        ComputeCallPathHash(access.Stack));
    
    private static int ComputeCallPathHash(CapturedStackTrace stack)
    {
        var deeperFrames = stack.GetDeeperFrames();
        if (deeperFrames.Count == 0)
            return 0;

        var hash = new HashCode();
        foreach (var frame in deeperFrames)
        {
            hash.Add(frame.ModuleId.Value);
            hash.Add(frame.MethodToken.Value);
        }

        return hash.ToHashCode();
    }

    public int CompareTo(RaceSite other)
    {
        var byModule = ModuleId.CompareTo(other.ModuleId);
        if (byModule != 0)
            return byModule;

        var byToken = MethodToken.CompareTo(other.MethodToken);
        if (byToken != 0)
            return byToken;

        var byOffset = MethodOffset.CompareTo(other.MethodOffset);
        if (byOffset != 0)
            return byOffset;

        var byAccessType = AccessType.CompareTo(other.AccessType);
        return byAccessType != 0
            ? byAccessType
            : CallPathHash.CompareTo(other.CallPathHash);
    }
}
