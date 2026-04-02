// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Core.Metadata;

namespace SharpDetect.Plugins.DataRace.Common;

public static class DataRaceLogger
{
    public static void LogDataRace(
        ILogger logger,
        DataRaceInfo raceInfo,
        IMetadataContext metadataContext)
    {
        var fieldName = GetFieldDisplayName(raceInfo.FieldId);
        var field = raceInfo.ObjectId is { } instance
            ? $"instance field {fieldName} on object {instance.ObjectId.Value}"
            : $"static field {fieldName}";
        
        logger.LogWarning(
            """
            [PID={ProcessId}] Data race on {Field}
                Current {CurrentAccessType} by thread {CurrentThread}:
                    at {CurrentAccessLocation}
                Previous {PreviousAccessType} by thread {PreviousThread}:
                    at {PreviousAccessLocation}
            """,
            raceInfo.ProcessId,
            field,
            GetAccessTypeDisplayString(raceInfo.CurrentAccess),
            raceInfo.CurrentAccess.ThreadName,
            GetAccessDisplayString(raceInfo.CurrentAccess, metadataContext),
            GetAccessTypeDisplayString(raceInfo.LastAccess),
            raceInfo.LastAccess.ThreadName,
            GetAccessDisplayString(raceInfo.LastAccess, metadataContext));
    }
    
    public static string GetFieldDisplayName(FieldId fieldId)
    {
        return $"{fieldId.FieldDef.DeclaringType.FullName}.{fieldId.FieldDef.Name}";
    }
    
    public static string GetFieldTitle(FieldId fieldId)
    {
        return $"{fieldId.FieldDef.DeclaringType.Name}.{fieldId.FieldDef.Name}";
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
    
    private static string GetAccessDisplayString(AccessInfo access, IMetadataContext metadataContext)
    {
        var pid = access.ProcessThreadId.ProcessId;
        var resolver = metadataContext.GetResolver(pid);
        var resolveResult = resolver.ResolveMethod(pid, access.ModuleId, access.MethodToken);
        return resolveResult.IsSuccess
            ? $"{resolveResult.Value.DeclaringType.FullName}.{resolveResult.Value.Name}:IL_{access.MethodOffset:X4}"
            : $"<unresolved-method>:IL_{access.MethodOffset:X4}";
    }
}

