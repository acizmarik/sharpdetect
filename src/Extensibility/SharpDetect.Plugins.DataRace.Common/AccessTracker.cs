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
        public Dictionary<ProcessThreadId, AccessRecord>? ReadsByThread;
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

    public bool TryGetLastReadAccess(
        FieldId fieldId,
        ProcessTrackedObjectId? objectId,
        ProcessThreadId readerThreadId,
        out AccessRecord record)
    {
        if (TryGetSlots(fieldId, objectId, out var slots))
        {
            if (slots.ReadsByThread != null && slots.ReadsByThread.TryGetValue(readerThreadId, out record))
                return true;

            if (slots.Last.AccessType == AccessType.Read && slots.Last.ProcessThreadId == readerThreadId)
            {
                record = slots.Last;
                return true;
            }
        }

        record = default;
        return false;
    }

    public AccessRecord RecordAccess(
        FieldId fieldId,
        ProcessTrackedObjectId? objectId,
        ProcessThreadId threadId,
        uint methodOffset,
        AccessType accessType,
        CapturedStackTrace stack,
        bool trackReadsByThread = false)
    {
        var record = new AccessRecord(threadId, methodOffset, accessType, stack);

        if (objectId != null)
        {
            if (!_instanceFieldSlots.TryGetValue(objectId.Value, out var fields))
            {
                fields = [];
                _instanceFieldSlots[objectId.Value] = fields;
            }

            ref var slots = ref CollectionsMarshal.GetValueRefOrAddDefault(fields, fieldId, out var exists);
            UpdateSlots(ref slots, exists, record, trackReadsByThread);
        }
        else
        {
            ref var slots = ref CollectionsMarshal.GetValueRefOrAddDefault(_staticFieldSlots, fieldId, out var exists);
            UpdateSlots(ref slots, exists, record, trackReadsByThread);
        }

        return record;
    }

    private static void UpdateSlots(ref AccessSlots slots, bool exists, in AccessRecord record, bool trackReadsByThread)
    {
        if (record.AccessType == AccessType.Write)
        {
            slots.LastWrite = record;
            slots.HasWrite = true;
            slots.ReadsByThread = null;
        }
        else if (trackReadsByThread)
        {
            var reads = slots.ReadsByThread ??= [];

            if (reads.Count == 0 &&
                exists &&
                slots.Last.AccessType == AccessType.Read &&
                slots.Last.ProcessThreadId != record.ProcessThreadId)
            {
                reads[slots.Last.ProcessThreadId] = slots.Last;
            }

            reads[record.ProcessThreadId] = record;
        }

        slots.Last = record;
    }

    private bool TryGetSlots(FieldId fieldId, ProcessTrackedObjectId? objectId, out AccessSlots slots)
    {
        if (objectId != null)
        {
            if (_instanceFieldSlots.TryGetValue(objectId.Value, out var fields) && fields.TryGetValue(fieldId, out slots))
                return true;
        }
        else if (_staticFieldSlots.TryGetValue(fieldId, out slots))
        {
            return true;
        }

        slots = default;
        return false;
    }
    
    public AccessInfo Materialize(in AccessRecord record)
    {
        return new AccessInfo(
            record.ProcessThreadId,
            threadNameResolver(record.ProcessThreadId),
            record.MethodOffset,
            record.AccessType,
            record.Stack);
    }

    public void RemoveTrackedObjects(uint processId, ReadOnlySpan<TrackedObjectId> removedObjectIds)
    {
        foreach (var objectId in removedObjectIds)
            _instanceFieldSlots.Remove(new ProcessTrackedObjectId(processId, objectId));
    }
}
