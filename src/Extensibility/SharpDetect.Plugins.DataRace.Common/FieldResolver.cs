// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using Microsoft.Extensions.Logging;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Metadata;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SharpDetect.Plugins.DataRace.Common;

public sealed class FieldResolver(IMetadataContext metadataContext, ILogger logger)
{
    private readonly record struct ResolvedFieldInfo(FieldDef FieldDef, FieldFlags Flags);
    private readonly Dictionary<FieldDefOrRef, ResolvedFieldInfo> _resolvedFields = [];
    private TypeRef? _delegateTypeRef;
    private TypeRef? _threadStaticAttributeTypeRef;
    private TypeRef? _asyncStateMachineTypeRef;
    private TypeRef? _taskTypeRef;
    private TypeRef? _continuationTypeRef;

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

    public static bool ShouldExcludeFromAnalysis(
        FieldFlags flags,
        IDataRacePluginConfiguration configuration)
    {
        return flags.HasFlag(FieldFlags.IsReadOnly) ||
               flags.HasFlag(FieldFlags.IsThreadStatic) ||
               flags.HasFlag(FieldFlags.IsAsyncStateMachineInternalField) ||
               flags.HasFlag(FieldFlags.IsTaskOrContinuationInternalField) ||
               (flags.HasFlag(FieldFlags.IsStaticDelegateType) && configuration.SuppressAnalysisOfStaticDelegates);
    }

    private FieldFlags ComputeFieldFlags(FieldDef fieldDef)
    {
        var flags = FieldFlags.None;

        if (IsFieldReadonly(fieldDef))
            flags |= FieldFlags.IsReadOnly;

        if (IsFieldThreadStatic(fieldDef))
            flags |= FieldFlags.IsThreadStatic;

        if (IsFieldInternalTaskStateMachineField(fieldDef))
            flags |= FieldFlags.IsAsyncStateMachineInternalField;

        if (IsFieldInternalTaskOrContinuationField(fieldDef))
            flags |= FieldFlags.IsTaskOrContinuationInternalField;

        if (IsStaticDelegateField(fieldDef))
            flags |= FieldFlags.IsStaticDelegateType;

        return flags;
    }

    private static bool IsFieldReadonly(FieldDef fieldDef)
    {
        return fieldDef.IsInitOnly || fieldDef.IsLiteral;
    }

    private bool IsFieldThreadStatic(FieldDef fieldDef)
    {
        _threadStaticAttributeTypeRef ??= fieldDef.Module.CorLibTypes
            .GetTypeRef(@namespace: "System", name: "ThreadStaticAttribute");

        var comparer = new SigComparer();
        return fieldDef.CustomAttributes.Any(a =>
            comparer.Equals(a.AttributeType, _threadStaticAttributeTypeRef));
    }

    private bool IsFieldInternalTaskStateMachineField(FieldDef fieldDef)
    {
        _asyncStateMachineTypeRef ??= fieldDef.Module.CorLibTypes
            .GetTypeRef(@namespace: "System.Runtime.CompilerServices", name: "IAsyncStateMachine");

        var comparer = new SigComparer();
        return fieldDef.DeclaringType.HasInterfaces && fieldDef.DeclaringType.Interfaces.Any(i =>
            comparer.Equals(i.Interface, _asyncStateMachineTypeRef));
    }

    private bool IsFieldInternalTaskOrContinuationField(FieldDef fieldDef)
    {
        _taskTypeRef ??= fieldDef.Module.CorLibTypes
            .GetTypeRef(@namespace: "System.Threading.Tasks", name: "Task");
        _continuationTypeRef ??= fieldDef.Module.CorLibTypes
            .GetTypeRef(@namespace: "System.Threading.Tasks", name: "TaskContinuation");

        var comparer = new SigComparer();
        ITypeDefOrRef? current = fieldDef.DeclaringType;
        while (current != null)
        {
            if (comparer.Equals(current, _taskTypeRef) ||
                comparer.Equals(current, _continuationTypeRef))
                return true;

            current = current.ResolveTypeDef()?.BaseType;
        }

        return false;
    }

    private bool IsStaticDelegateField(FieldDef fieldDef)
    {
        return fieldDef.IsStatic && IsDelegateField(fieldDef);
    }

    private bool IsDelegateField(FieldDef fieldDef)
    {
        _delegateTypeRef ??= fieldDef.Module.CorLibTypes
            .GetTypeRef(@namespace: "System", name: "Delegate");

        var comparer = new SigComparer();
        ITypeDefOrRef? current = fieldDef.FieldType.ToTypeDefOrRef();
        while (current != null)
        {
            if (comparer.Equals(current, _delegateTypeRef))
                return true;

            current = current.ResolveTypeDef()?.BaseType;
        }

        return false;
    }
}

