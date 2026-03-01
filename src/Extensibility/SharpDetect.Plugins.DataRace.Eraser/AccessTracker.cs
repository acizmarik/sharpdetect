// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.Eraser;

internal sealed class AccessTracker(TimeProvider timeProvider, Func<ProcessThreadId, string?> threadNameResolver)
{
    private readonly Dictionary<FieldId, AccessInfo> _staticLastAccessInfo = [];
    private readonly Dictionary<FieldId, AccessInfo> _staticLastWriteAccessInfo = [];
    private readonly Dictionary<(FieldId, ProcessTrackedObjectId), AccessInfo> _instanceLastAccessInfo = [];
    private readonly Dictionary<(FieldId, ProcessTrackedObjectId), AccessInfo> _instanceLastWriteAccessInfo = [];
    private readonly Dictionary<ProcessTrackedObjectId, HashSet<FieldId>> _objectFieldIndex = [];
    
    public AccessInfo? GetLastAccess(FieldId fieldId, ProcessTrackedObjectId? objectId)
    {
        return objectId != null
            ? _instanceLastAccessInfo.GetValueOrDefault((fieldId, objectId.Value))
            : _staticLastAccessInfo.GetValueOrDefault(fieldId);
    }
    
    public AccessInfo? GetLastWriteAccess(FieldId fieldId, ProcessTrackedObjectId? objectId)
    {
        return objectId != null
            ? _instanceLastWriteAccessInfo.GetValueOrDefault((fieldId, objectId.Value))
            : _staticLastWriteAccessInfo.GetValueOrDefault(fieldId);
    }

    public void RecordAccess(FieldId fieldId, ProcessTrackedObjectId? objectId, AccessInfo accessInfo)
    {
        if (objectId != null)
        {
            var key = (fieldId, objectId.Value);
            _instanceLastAccessInfo[key] = accessInfo;
            if (accessInfo.AccessType == AccessType.Write)
                _instanceLastWriteAccessInfo[key] = accessInfo;
            TrackObjectField(objectId.Value, fieldId);
        }
        else
        {
            _staticLastAccessInfo[fieldId] = accessInfo;
            if (accessInfo.AccessType == AccessType.Write)
                _staticLastWriteAccessInfo[fieldId] = accessInfo;
        }
    }
    
    public AccessInfo CreateAccessInfo(
        ProcessThreadId threadId,
        Core.Events.Profiler.ModuleId moduleId,
        Core.Events.Profiler.MdMethodDef methodToken,
        AccessType accessType)
    {
        return new AccessInfo(
            threadId,
            threadNameResolver(threadId),
            moduleId,
            methodToken,
            accessType,
            timeProvider.GetUtcNow().DateTime);
    }
    
    public void RemoveTrackedObjects(uint processId, ReadOnlySpan<TrackedObjectId> removedObjectIds)
    {
        foreach (var objectId in removedObjectIds)
        {
            var processObjectId = new ProcessTrackedObjectId(processId, objectId);
            if (!_objectFieldIndex.Remove(processObjectId, out var fieldIds))
                continue;
            
            foreach (var fieldId in fieldIds)
            {
                _instanceLastAccessInfo.Remove((fieldId, processObjectId));
                _instanceLastWriteAccessInfo.Remove((fieldId, processObjectId));
            }
        }
    }
    
    private void TrackObjectField(ProcessTrackedObjectId objectId, FieldId fieldId)
    {
        if (!_objectFieldIndex.TryGetValue(objectId, out var fieldIds))
        {
            fieldIds = [];
            _objectFieldIndex[objectId] = fieldIds;
        }

        fieldIds.Add(fieldId);
    }
}
