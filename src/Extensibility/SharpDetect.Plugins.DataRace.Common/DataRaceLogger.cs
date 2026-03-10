// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.Common;

public static class DataRaceLogger
{
    public static void LogDataRace(
        ILogger logger,
        IReadOnlyDictionary<ProcessThreadId, string> threads,
        ProcessThreadId reporterThreadId,
        DataRaceInfo raceInfo)
    {
        var fieldName = GetFieldDisplayName(raceInfo.FieldId);
        var field = raceInfo.ObjectId != null
            ? $"instance field {fieldName} (obj={raceInfo.ObjectId})"
            : $"static field {fieldName}";
        var currentThread = threads[reporterThreadId];
        var lastThread = raceInfo.LastAccess.ThreadName;

        logger.LogWarning(
            "[PID={ProcessId}] Data race on {Field}; type: {RaceCategory}; previous thread: {LastThread}; current thread: {Thread}",
            raceInfo.ProcessId,
            field,
            GetRaceCategory(raceInfo),
            lastThread,
            currentThread);
    }
    
    public static string GetFieldDisplayName(FieldId fieldId)
    {
        return $"{fieldId.FieldDef.DeclaringType.FullName}.{fieldId.FieldDef.Name}";
    }
    
    public static string GetRaceCategory(DataRaceInfo raceInfo)
    {
        var lastAccessType = raceInfo.LastAccess.AccessType;
        var currentAccessType = raceInfo.CurrentAccess.AccessType;
        return (lastAccessType, currentAccessType) switch
        {
            (AccessType.Read, AccessType.Write) => "read-write",
            (AccessType.Write, AccessType.Read) => "write-read",
            (AccessType.Write, AccessType.Write) => "write-write",
            _ => "unclassified"
        };
    }
}

