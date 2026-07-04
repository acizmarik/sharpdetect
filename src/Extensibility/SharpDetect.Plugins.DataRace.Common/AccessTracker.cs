// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.Common;

public sealed class AccessTracker(Func<ProcessThreadId, string?> threadNameResolver)
{
    private readonly Dictionary<FieldId, AccessSlots> _staticFieldSlots = [];
    private readonly Dictionary<ProcessTrackedObjectId, Dictionary<FieldId, AccessSlots>> _instanceFieldSlots = [];

    private struct AccessSlots
    {
        public AccessRecord Last;
        public AccessRecord LastWrite;
        public bool HasWrite;
    }

    public bool TryGetLastAccess(FieldId fieldId, ProcessTrackedObjectId? objectId, out AccessRecord record)
    {
        if (objectId != null)
        {
            if (_instanceFieldSlots.TryGetValue(objectId.Value, out var fields) && fields.TryGetValue(fieldId, out var slots))
            {
                record = slots.Last;
                return true;
            }
        }
        else if (_staticFieldSlots.TryGetValue(fieldId, out var slots))
        {
            record = slots.Last;
            return true;
        }

        record = default;
        return false;
    }

    public bool TryGetLastWriteAccess(FieldId fieldId, ProcessTrackedObjectId? objectId, out AccessRecord record)
    {
        if (objectId != null)
        {
            if (_instanceFieldSlots.TryGetValue(objectId.Value, out var fields) && fields.TryGetValue(fieldId, out var slots) && slots.HasWrite)
            {
                record = slots.LastWrite;
                return true;
            }
        }
        else if (_staticFieldSlots.TryGetValue(fieldId, out var slots) && slots.HasWrite)
        {
            record = slots.LastWrite;
            return true;
        }

        record = default;
        return false;
    }

    public AccessRecord RecordAccess(
        FieldId fieldId,
        ProcessTrackedObjectId? objectId,
        ProcessThreadId threadId,
        ModuleId moduleId,
        MdMethodDef methodToken,
        uint methodOffset,
        AccessType accessType)
    {
        var record = new AccessRecord(threadId, moduleId, methodToken, methodOffset, accessType);
        var isWrite = accessType == AccessType.Write;

        if (objectId != null)
        {
            if (!_instanceFieldSlots.TryGetValue(objectId.Value, out var fields))
            {
                fields = [];
                _instanceFieldSlots[objectId.Value] = fields;
            }

            ref var slots = ref CollectionsMarshal.GetValueRefOrAddDefault(fields, fieldId, out _);
            slots.Last = record;
            if (isWrite)
            {
                slots.LastWrite = record;
                slots.HasWrite = true;
            }
        }
        else
        {
            ref var slots = ref CollectionsMarshal.GetValueRefOrAddDefault(_staticFieldSlots, fieldId, out _);
            slots.Last = record;
            if (isWrite)
            {
                slots.LastWrite = record;
                slots.HasWrite = true;
            }
        }

        return record;
    }
    
    public AccessInfo Materialize(in AccessRecord record)
    {
        return new AccessInfo(
            record.ProcessThreadId,
            threadNameResolver(record.ProcessThreadId),
            record.ModuleId,
            record.MethodToken,
            record.MethodOffset,
            record.AccessType);
    }

    public void RemoveTrackedObjects(uint processId, ReadOnlySpan<TrackedObjectId> removedObjectIds)
    {
        foreach (var objectId in removedObjectIds)
            _instanceFieldSlots.Remove(new ProcessTrackedObjectId(processId, objectId));
    }
}
