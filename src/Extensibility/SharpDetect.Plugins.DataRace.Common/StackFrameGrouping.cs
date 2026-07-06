// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Plugins.DataRace.Common;

public readonly record struct StackFrameRun(bool IsSystemRun, IReadOnlyList<StackFrame> Frames);

public static class StackFrameGrouping
{
    public static IReadOnlyList<StackFrameRun> GroupSystemFrameRuns(IReadOnlyList<StackFrame> frames)
    {
        var runs = new List<StackFrameRun>();
        var index = 0;
        while (index < frames.Count)
        {
            var isSystemRun = index > 0 && WellKnownModules.IsSystemModule(frames[index].SourceMapping);
            var end = index + 1;
            while (isSystemRun && end < frames.Count && WellKnownModules.IsSystemModule(frames[end].SourceMapping))
                end++;

            var runFrames = new List<StackFrame>(end - index);
            for (var i = index; i < end; i++)
                runFrames.Add(frames[i]);

            runs.Add(new StackFrameRun(isSystemRun, runFrames));
            index = end;
        }

        return runs;
    }
}
