// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Core.Metadata;
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
        var field = raceInfo.ObjectId is { } instance
            ? $"instance field {fieldName} on object {instance.ObjectId.Value}"
            : $"static field {fieldName}";
        
        logger.LogWarning(
            """
            [PID={ProcessId}] Data race on {Field}
                Current {CurrentAccessType} by thread {CurrentThread}:
            {CurrentAccessStackTrace}
                Previous {PreviousAccessType} by thread {PreviousThread}:
            {PreviousAccessStackTrace}
            """,
            raceInfo.ProcessId,
            field,
            GetAccessTypeDisplayString(raceInfo.CurrentAccess),
            GetThreadDisplayName(raceInfo.CurrentAccess),
            FormatAccessStackTrace(raceInfo.CurrentAccess, metadataContext, symbolResolver),
            GetAccessTypeDisplayString(raceInfo.LastAccess),
            GetThreadDisplayName(raceInfo.LastAccess),
            FormatAccessStackTrace(raceInfo.LastAccess, metadataContext, symbolResolver));
    }
    
    public static string GetFieldDisplayName(FieldId fieldId)
    {
        return $"{fieldId.FieldDef.DeclaringType.FullName}.{fieldId.FieldDef.Name}";
    }
    
    public static string GetFieldTitle(FieldId fieldId)
    {
        return $"{fieldId.FieldDef.DeclaringType.Name}.{fieldId.FieldDef.Name}";
    }

    public static string GetThreadDisplayName(AccessInfo access)
    {
        return access.ThreadName ?? $"Thread-0x{access.ProcessThreadId.ThreadId.Value:X}";
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

    private static string FormatFrameLocation(StackFrame frame)
    {
        if (frame.SourceFileName is null || frame.SourceLine is null)
        {
            return frame.MethodOffset is { } offset
                ? $"{frame.MethodName}:IL_{offset:X4}"
                : frame.MethodName;
        }
        
        return $"{frame.MethodName} in {frame.SourceFileName}:{frame.SourceLine}";
    }
}
