// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using Microsoft.Extensions.Logging;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Metadata;
using SharpDetect.Core.Plugins;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SharpDetect.Plugins.DataRace.Eraser;

internal sealed class EraserDetector
{
    private readonly record struct FieldInfo(FieldDef FieldDef, FieldFlags Flags);
    
    private readonly ShadowMemory _shadowMemory = new();
    private readonly LockSetTable _lockSetTable = new();
    private readonly Dictionary<ProcessThreadId, LockSetIndex> _threadLockSets = [];
    private readonly Dictionary<FieldId, AccessInfo> _lastAccessInfo = [];
    private readonly Dictionary<FieldDefOrRef, FieldInfo> _resolvedFields = [];
    private readonly TimeProvider _timeProvider;
    private readonly IMetadataContext _metadataContext;
    private readonly ILogger _logger;
    private readonly Func<ProcessThreadId, string?> _threadNameResolver;

    public EraserDetector(
        IMetadataContext metadataContext,
        TimeProvider timeProvider,
        ILogger logger,
        Func<ProcessThreadId, string?> threadNameResolver)
    {
        _metadataContext = metadataContext;
        _timeProvider = timeProvider;
        _logger = logger;
        _threadNameResolver = threadNameResolver;
    }
    
    public int GetDistinctLockSetCount() => _lockSetTable.Count;

    public int GetTrackedFieldCount() => _shadowMemory.Count;
    
    public void RecordThreadCreated(ProcessThreadId threadId)
    {
        _threadLockSets[threadId] = LockSetIndex.Empty;
    }
    
    public void RecordThreadDestroyed(ProcessThreadId threadId)
    {
        _threadLockSets.Remove(threadId);
    }

    public void RecordLockAcquired(ProcessThreadId threadId, ProcessTrackedObjectId lockId)
    {
        var currentLockSet = GetThreadLockSet(threadId);
        var newLockSet = _lockSetTable.Add(currentLockSet, lockId);
        _threadLockSets[threadId] = newLockSet;
    }
    
    public void RecordLockReleased(ProcessThreadId threadId, ProcessTrackedObjectId lockId)
    {
        var currentLockSet = GetThreadLockSet(threadId);
        var newLockSet = _lockSetTable.Remove(currentLockSet, lockId);
        _threadLockSets[threadId] = newLockSet;
    }
    
    public void RecordObjectWaitCalled(ProcessThreadId threadId, ProcessTrackedObjectId lockId)
    {
        var currentLockSet = GetThreadLockSet(threadId);
        var newLockSet = _lockSetTable.Remove(currentLockSet, lockId);
        _threadLockSets[threadId] = newLockSet;
    }
    
    public void RecordObjectWaitReturned(ProcessThreadId threadId, ProcessTrackedObjectId lockId)
    {
        var currentLockSet = GetThreadLockSet(threadId);
        var newLockSet = _lockSetTable.Add(currentLockSet, lockId);
        _threadLockSets[threadId] = newLockSet;
    }
    
    public DataRaceInfo? RecordRead(
        ProcessThreadId threadId,
        ModuleId moduleId,
        MdMethodDef methodToken,
        MdToken fieldToken)
    {
        return RecordAccess(
            threadId,
            moduleId,
            methodToken,
            fieldToken,
            isWrite: false);
    }
    
    public DataRaceInfo? RecordWrite(
        ProcessThreadId threadId,
        ModuleId moduleId,
        MdMethodDef methodToken,
        MdToken fieldToken)
    {
        return RecordAccess(
            threadId,
            moduleId,
            methodToken,
            fieldToken,
            isWrite: true);
    }

    private DataRaceInfo? RecordAccess(
        ProcessThreadId threadId,
        ModuleId moduleId,
        MdMethodDef methodToken,
        MdToken fieldToken,
        bool isWrite)
    {
        if (!TryResolveField(threadId.ProcessId, moduleId, fieldToken, out var fieldInfo))
        {
            _logger.LogWarning("Skipping analysis of field with token={FieldToken} in module {ModuleId} because it could not be resolved",
                fieldToken.Value, moduleId);
            return null;
        }

        if (fieldInfo.Flags.HasFlag(FieldFlags.IsReadOnly) ||
            fieldInfo.Flags.HasFlag(FieldFlags.IsThreadStatic))
        {
            // Readonly and thread-static fields cannot be involved in data races
            return null;
        }

        var fieldId = new FieldId(threadId.ProcessId, fieldInfo.FieldDef);
        var shadow = _shadowMemory.GetOrCreateVirgin(fieldId);
        var threadLockSet = GetThreadLockSet(threadId);
        var lastAccess = _lastAccessInfo.GetValueOrDefault(fieldId);
        var accessType = isWrite ? AccessType.Write : AccessType.Read;

        var (newShadow, raceInfo) = ComputeStateTransition(
            threadId,
            fieldId,
            shadow,
            threadLockSet,
            moduleId,
            methodToken,
            lastAccess,
            isWrite);

        _shadowMemory.Update(fieldId, newShadow);
        UpdateLastAccess(fieldId, threadId, moduleId, methodToken, accessType);

        return raceInfo;
    }

    private (ShadowVariable NewShadow, DataRaceInfo? RaceInfo) ComputeStateTransition(
        ProcessThreadId threadId,
        FieldId fieldId,
        ShadowVariable shadow,
        LockSetIndex threadLockSet,
        ModuleId moduleId,
        MdMethodDef methodToken,
        AccessInfo? lastAccess,
        bool isWrite)
    {
        return shadow.State switch
        {
            ShadowVariableState.Virgin => (ShadowVariable.CreateExclusive(threadId, threadLockSet), RaceInfo: null),
            ShadowVariableState.Exclusive when shadow.ExclusiveThread == threadId => (shadow, RaceInfo: null),
            ShadowVariableState.Exclusive => TransitionFromExclusive(
                threadId, fieldId, shadow, threadLockSet, moduleId, methodToken, lastAccess, isWrite),
            ShadowVariableState.Shared or ShadowVariableState.SharedModified =>
                TransitionInSharedState(
                    threadId, fieldId, shadow, threadLockSet, moduleId, methodToken, lastAccess, isWrite),

            _ => throw new InvalidOperationException($"Unknown state: {shadow.State}")
        };
    }

    private (ShadowVariable NewShadow, DataRaceInfo? RaceInfo) TransitionFromExclusive(
        ProcessThreadId threadId,
        FieldId fieldId,
        ShadowVariable shadow,
        LockSetIndex threadLockSet,
        ModuleId moduleId,
        MdMethodDef methodToken,
        AccessInfo? lastAccess,
        bool isWrite)
    {
        var newLockSet = _lockSetTable.Intersect(shadow.LockSetIndex, threadLockSet);
        var accessType = isWrite ? AccessType.Write : AccessType.Read;
        var newState = accessType == AccessType.Read
            ? ShadowVariableState.Shared
            : ShadowVariableState.SharedModified;
        var newShadowVariable = newState == ShadowVariableState.SharedModified
            ? ShadowVariable.CreateSharedModified(newLockSet)
            : ShadowVariable.CreateShared(newLockSet);
        var deadlockInfo = newLockSet.IsEmpty
            ? CreateRaceInfo(threadId, fieldId, moduleId, methodToken,
                accessType, ShadowVariableState.Exclusive, newState, newLockSet, lastAccess)
            : null;

        return (newShadowVariable, deadlockInfo);
    }

    private (ShadowVariable NewShadow, DataRaceInfo? RaceInfo) TransitionInSharedState(
        ProcessThreadId threadId,
        FieldId fieldId,
        ShadowVariable shadow,
        LockSetIndex threadLockSet,
        ModuleId moduleId,
        MdMethodDef methodToken,
        AccessInfo? lastAccess,
        bool isWrite)
    {
        var newLockSet = _lockSetTable.Intersect(shadow.LockSetIndex, threadLockSet);

        var newState = isWrite ? ShadowVariableState.SharedModified : shadow.State;
        var newShadow = newState == ShadowVariableState.SharedModified
            ? ShadowVariable.CreateSharedModified(newLockSet)
            : ShadowVariable.CreateShared(newLockSet);

        var raceInfo = newLockSet.IsEmpty
            ? CreateRaceInfo(threadId, fieldId, moduleId, methodToken,
                isWrite ? AccessType.Write : AccessType.Read, shadow.State, newState, newLockSet, lastAccess)
            : null;

        return (newShadow, raceInfo);
    }

    private LockSetIndex GetThreadLockSet(ProcessThreadId threadId)
    {
        return _threadLockSets.GetValueOrDefault(threadId, LockSetIndex.Empty);
    }

    private void UpdateLastAccess(
        FieldId fieldId,
        ProcessThreadId threadId,
        ModuleId moduleId,
        MdMethodDef methodToken,
        AccessType accessType)
    {
        var accessInfo = new AccessInfo(
            threadId,
            _threadNameResolver(threadId),
            moduleId,
            methodToken,
            accessType,
            _timeProvider.GetUtcNow().DateTime);
        
        _lastAccessInfo[fieldId] = accessInfo;
    }

    private DataRaceInfo CreateRaceInfo(
        ProcessThreadId threadId,
        FieldId fieldId,
        ModuleId moduleId,
        MdMethodDef methodToken,
        AccessType accessType,
        ShadowVariableState previousState,
        ShadowVariableState newState,
        LockSetIndex candidateLockSet,
        AccessInfo? lastAccess)
    {
        var currentAccess = new AccessInfo(
            threadId,
            _threadNameResolver(threadId),
            moduleId,
            methodToken,
            accessType,
            _timeProvider.GetUtcNow().DateTime);

        return new DataRaceInfo(
            ProcessId: threadId.ProcessId,
            FieldId: fieldId,
            CurrentAccess: currentAccess,
            LastAccess: lastAccess,
            PreviousState: previousState,
            NewState: newState,
            CandidateLockSet: candidateLockSet,
            Timestamp: _timeProvider.GetUtcNow().DateTime);
    }
    
    private bool TryResolveField(
        uint processId,
        ModuleId moduleId,
        MdToken fieldToken,
        out FieldInfo fieldInfo)
    {
        var fieldDefOrRef = new FieldDefOrRef(moduleId, fieldToken);
        if (_resolvedFields.TryGetValue(fieldDefOrRef, out fieldInfo))
            return true;
        
        var resolver = _metadataContext.GetResolver(processId);
        var resolveResult = resolver.ResolveField(processId, moduleId, fieldToken);
        if (resolveResult.IsError)
            return false;

        var fieldDef = resolveResult.Value;
        var fieldFlags = FieldFlags.None;
        if (IsFieldReadonly(fieldDef))
            fieldFlags |= FieldFlags.IsReadOnly;
        if (IsFieldThreadStatic(fieldDef))
            fieldFlags |= FieldFlags.IsThreadStatic;
        
        _resolvedFields.Add(fieldDefOrRef, new FieldInfo(fieldDef, fieldFlags));
        fieldInfo = new FieldInfo(fieldDef, fieldFlags);

        return true;
    }
    
    private static bool IsFieldReadonly(FieldDef fieldDef)
    {
        return fieldDef.IsInitOnly || fieldDef.IsLiteral;
    }
    
    private static bool IsFieldThreadStatic(FieldDef fieldDef)
    {
        return fieldDef.CustomAttributes.Any(a => a.AttributeType.FullName.Equals(
            "System.ThreadStaticAttribute", StringComparison.Ordinal));
    }
}
