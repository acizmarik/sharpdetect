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
    private readonly Dictionary<FieldDefOrRef, ResolvedFieldInfo?> _resolvedFields = [];
    private TypeRef? _threadStaticAttributeTypeRef;
    private TypeRef? _compilerGeneratedAttributeTypeRef;
    private TypeRef? _multicastDelegateTypeRef;
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
        var fieldDefOrRef = new FieldDefOrRef(processId, moduleId, fieldToken);

        if (_resolvedFields.TryGetValue(fieldDefOrRef, out var cached))
        {
            if (cached is not { } cachedInfo)
            {
                fieldDef = null;
                fieldFlags = FieldFlags.None;
                return false;
            }

            fieldDef = cachedInfo.FieldDef;
            fieldFlags = cachedInfo.Flags;
            return true;
        }

        var resolver = metadataContext.GetResolver(processId);
        var resolveResult = resolver.ResolveField(processId, moduleId, fieldToken);

        if (resolveResult.IsError)
        {
            if (resolveResult.Error != MetadataResolverErrorType.ModuleDynamicallyGenerated)
            {
                logger.LogWarning(
                    "Skipping analysis of field with token={FieldToken} in module {ModuleId} because it could not be resolved",
                    fieldToken.Value,
                    moduleId);
            }

            _resolvedFields.Add(fieldDefOrRef, null);
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
               flags.HasFlag(FieldFlags.IsStaticDelegateType);
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

        if (IsFieldDelegateCacheInCompilerGeneratedType(fieldDef))
            flags |= FieldFlags.IsStaticDelegateType;

        if (IsAutoPropertyBackingField(fieldDef))
            flags |= FieldFlags.IsAutoPropertyBackingField;

        return flags;
    }

    private bool IsAutoPropertyBackingField(FieldDef fieldDef)
    {
        const string backingFieldNameSuffix = "k__BackingField";

        var name = fieldDef.Name?.String;
        if (name is null || !name.EndsWith(backingFieldNameSuffix, StringComparison.Ordinal))
            return false;

        return HasCompilerGeneratedAttribute(fieldDef, fieldDef.Module);
    }

    private bool HasCompilerGeneratedAttribute(IHasCustomAttribute member, ModuleDef module)
    {
        _compilerGeneratedAttributeTypeRef ??= module.CorLibTypes.GetTypeRef(
            @namespace: "System.Runtime.CompilerServices",
            name: "CompilerGeneratedAttribute");

        var comparer = new SigComparer();
        return member.CustomAttributes.Any(a => comparer.Equals(a.AttributeType, _compilerGeneratedAttributeTypeRef));
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

    private bool IsFieldDelegateCacheInCompilerGeneratedType(FieldDef fieldDef)
    {
        var declaringType = fieldDef.DeclaringType;
        if (!fieldDef.IsStatic ||
            fieldDef.IsInitOnly ||
            !declaringType.IsNested ||
            declaringType.HasInterfaces)
        {
            return false;
        }

        if (!HasCompilerGeneratedAttribute(declaringType, fieldDef.Module))
            return false;

        _multicastDelegateTypeRef ??= fieldDef.Module.CorLibTypes
            .GetTypeRef(@namespace: "System", name: "MulticastDelegate");

        var sigComparer = new SigComparer();
        var fieldType = fieldDef.FieldType.ToTypeDefOrRef();
        while (fieldType != null)
        {
            if (sigComparer.Equals(fieldType, _multicastDelegateTypeRef))
                return true;

            fieldType = fieldType.ResolveTypeDef()?.BaseType;
        }

        return false;
    }
}

