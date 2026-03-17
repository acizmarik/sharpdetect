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
            "[PID={ProcessId}] Data race on {Field}; previous thread: {LastThread}; current thread: {Thread}",
            raceInfo.ProcessId,
            field,
            lastThread,
            currentThread);
    }
    
    public static string GetFieldDisplayName(FieldId fieldId)
    {
        return $"{fieldId.FieldDef.DeclaringType.FullName}.{fieldId.FieldDef.Name}";
    }
    
    public static string GetFieldTitle(FieldId fieldId)
    {
        return $"{fieldId.FieldDef.DeclaringType.Name}.{fieldId.FieldDef.Name}";
    }
}

