// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Core.Metadata;
using SharpDetect.Core.Reporting.Formatters;
using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Plugins.DataRace.Common;

public static class DataRaceLogger
{
    public static void LogDataRace(
        ILogger logger,
        DataRaceInfo raceInfo,
        IMetadataContext metadataContext,
        ISymbolResolver symbolResolver)
    {
        var fieldName = GetFieldDisplayName(raceInfo.FieldId);
        var field = raceInfo.ObjectId is not null
            ? $"instance field {fieldName}"
            : $"static field {fieldName}";
        
        logger.LogWarning(
            """
            [PID={ProcessId}] Data race on {Field}
                Earlier {EarlierAccessType} by thread {EarlierThread}:
            {EarlierAccessStackTrace}
                Later {LaterAccessType} by thread {LaterThread}:
            {LaterAccessStackTrace}
            """,
            raceInfo.ProcessId,
            field,
            GetAccessTypeDisplayString(raceInfo.LastAccess),
            GetThreadDisplayName(raceInfo.LastAccess),
            FormatAccessStackTrace(raceInfo.LastAccess, metadataContext, symbolResolver),
            GetAccessTypeDisplayString(raceInfo.CurrentAccess),
            GetThreadDisplayName(raceInfo.CurrentAccess),
            FormatAccessStackTrace(raceInfo.CurrentAccess, metadataContext, symbolResolver));
    }

    public static void LogDataRaceLine(
        ILogger logger,
        DataRaceInfo raceInfo,
        IMetadataContext metadataContext,
        ISymbolResolver symbolResolver)
    {
        var processId = raceInfo.ProcessId;
        var earlierFrames = DataRaceStackTraceResolver.ResolveFrames(processId, raceInfo.LastAccess, metadataContext, symbolResolver);
        var laterFrames = DataRaceStackTraceResolver.ResolveFrames(processId, raceInfo.CurrentAccess, metadataContext, symbolResolver);
        logger.LogWarning("{RaceLine}", FormatDataRaceLine(raceInfo, earlierFrames, laterFrames));
    }

    public static string FormatDataRaceLine(
        DataRaceInfo raceInfo,
        IReadOnlyList<StackFrame> earlierFrames,
        IReadOnlyList<StackFrame> laterFrames)
    {
        return $"data race on {GetFieldDisplayName(raceInfo.FieldId)}: " +
               $"{FormatShortAccess(raceInfo.LastAccess, earlierFrames)} ↔ {FormatShortAccess(raceInfo.CurrentAccess, laterFrames)}";
    }
    
    public static string GetFieldDisplayName(FieldId fieldId)
    {
        return $"{fieldId.FieldDef.DeclaringType.FullName}.{fieldId.FieldDef.Name}";
    }

    public static string GetThreadDisplayName(AccessInfo access)
    {
        return access.ThreadName ?? $"Thread#{access.ProcessThreadId.ThreadId.Value}";
    }

    public static IReadOnlyList<string> FormatStackTraceLines(IReadOnlyList<StackFrame> frames)
    {
        var lines = new List<string>();
        foreach (var run in StackFrameGrouping.GroupSystemFrameRuns(frames))
        {
            if (run.IsSystemRun)
            {
                var skipped = run.Frames.Count - 1;
                lines.Add(skipped > 0
                    ? $"at {run.Frames[0].MethodName} (+{skipped} more)"
                    : $"at {run.Frames[0].MethodName}");
            }
            else
            {
                lines.Add($"at {FormatFrameLocation(run.Frames[0])}");
            }
        }

        return lines;
    }

    private static string GetAccessTypeDisplayString(AccessInfo access)
    {
        return access.AccessType switch
        {
            AccessType.Read => "read",
            AccessType.Write => "write",
            AccessType.AtomicRead => "atomic read",
            AccessType.AtomicWrite => "atomic write",
            _ => "<unresolved-access-type>"
        };
    }

    private static string FormatAccessStackTrace(
        AccessInfo access,
        IMetadataContext metadataContext,
        ISymbolResolver symbolResolver)
    {
        var frames = DataRaceStackTraceResolver.ResolveFrames(
            access.ProcessThreadId.ProcessId, access, metadataContext, symbolResolver);
        return string.Join(Environment.NewLine, FormatStackTraceLines(frames).Select(line => $"        {line}"));
    }

    private static string FormatShortAccess(AccessInfo access, IReadOnlyList<StackFrame> frames)
    {
        var location = frames.Count == 0 ? "<unresolved-method>"
            : frames[0].Source is { } source ? SourceLocationFormatter.FormatShort(source)
            : frames[0].MethodName;
        return $"{GetAccessTypeDisplayString(access)} by {GetThreadDisplayName(access)} at {location}";
    }

    private static string FormatFrameLocation(StackFrame frame)
    {
        if (frame.Source is { } source)
            return $"{frame.MethodName} in {SourceLocationFormatter.FormatFull(source)}";

        return frame.Il is { } il
            ? $"{frame.MethodName}:{InstructionsFormatter.FormatIlOffset(il.Offset)}"
            : frame.MethodName;
    }
}
