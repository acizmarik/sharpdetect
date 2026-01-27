// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using Microsoft.Extensions.Logging;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Metadata;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SharpDetect.Plugins.DataRace.Eraser;

internal sealed class FieldResolver(IMetadataContext metadataContext, ILogger logger)
{
    private readonly record struct ResolvedFieldInfo(FieldDef FieldDef, FieldFlags Flags);
    private readonly Dictionary<FieldDefOrRef, ResolvedFieldInfo> _resolvedFields = [];

    public bool TryResolve(
        uint processId,
        ModuleId moduleId,
        MdToken fieldToken,
        out FieldDef? fieldDef,
        out FieldFlags fieldFlags)
    {
        var fieldDefOrRef = new FieldDefOrRef(moduleId, fieldToken);
        
        if (_resolvedFields.TryGetValue(fieldDefOrRef, out var cached))
        {
            fieldDef = cached.FieldDef;
            fieldFlags = cached.Flags;
            return true;
        }

        var resolver = metadataContext.GetResolver(processId);
        var resolveResult = resolver.ResolveField(processId, moduleId, fieldToken);
        
        if (resolveResult.IsError)
        {
            logger.LogWarning(
                "Skipping analysis of field with token={FieldToken} in module {ModuleId} because it could not be resolved",
                fieldToken.Value, 
                moduleId);
            
            fieldDef = null;
            fieldFlags = FieldFlags.None;
            return false;
        }

        fieldDef = resolveResult.Value;
        fieldFlags = ComputeFieldFlags(fieldDef);
        
        _resolvedFields.Add(fieldDefOrRef, new ResolvedFieldInfo(fieldDef, fieldFlags));
        return true;
    }
    
    public static bool ShouldExcludeFromAnalysis(FieldFlags flags)
    {
        // Readonly and thread-static fields cannot be involved in data races
        return flags.HasFlag(FieldFlags.IsReadOnly) || 
               flags.HasFlag(FieldFlags.IsThreadStatic);
    }

    private static FieldFlags ComputeFieldFlags(FieldDef fieldDef)
    {
        var flags = FieldFlags.None;
        
        if (IsFieldReadonly(fieldDef))
            flags |= FieldFlags.IsReadOnly;
        
        if (IsFieldThreadStatic(fieldDef))
            flags |= FieldFlags.IsThreadStatic;
        
        return flags;
    }

    private static bool IsFieldReadonly(FieldDef fieldDef)
    {
        return fieldDef.IsInitOnly || fieldDef.IsLiteral;
    }

    private static bool IsFieldThreadStatic(FieldDef fieldDef)
    {
        return fieldDef.CustomAttributes.Any(a => 
            a.AttributeType.FullName.Equals("System.ThreadStaticAttribute", StringComparison.Ordinal));
    }
}
