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
        public bool HasLast;
        public bool HasWrite;
        public Dictionary<ProcessThreadId, AccessRecord>? ReadsByThread;
        public Dictionary<ProcessThreadId, AtomicAccessRecords>? AtomicByThread;
    }

    private readonly record struct AtomicAccessRecords(AccessRecord Last, AccessRecord? LastStore);

    public bool TryGetLastAccess(FieldId fieldId, ProcessTrackedObjectId? objectId, out AccessRecord record)
    {
        if (TryGetSlots(fieldId, objectId, out var slots) && slots.HasLast)
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

    public void CopyLastReadAccesses(
        FieldId fieldId,
        ProcessTrackedObjectId? objectId,
        Dictionary<ProcessThreadId, AccessRecord> destination)
    {
        if (!TryGetSlots(fieldId, objectId, out var slots))
            return;

        if (slots.ReadsByThread != null)
        {
            foreach (var (threadId, record) in slots.ReadsByThread)
                destination[threadId] = record;
        }
        else if (slots.HasLast && slots.Last.AccessType == AccessType.Read)
        {
            destination[slots.Last.ProcessThreadId] = slots.Last;
        }
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
        UpdateSlots(ref GetOrAddSlots(fieldId, objectId), record, trackReadsByThread);
        return record;
    }

    public AccessRecord RecordAtomicAccess(
        FieldId fieldId,
        ProcessTrackedObjectId? objectId,
        ProcessThreadId threadId,
        uint methodOffset,
        bool stored,
        CapturedStackTrace stack)
    {
        var record = new AccessRecord(threadId, methodOffset, stored ? AccessType.AtomicWrite : AccessType.AtomicRead, stack);
        ref var slots = ref GetOrAddSlots(fieldId, objectId);
        var atomicByThread = slots.AtomicByThread ??= [];
        ref var records = ref CollectionsMarshal.GetValueRefOrAddDefault(atomicByThread, threadId, out _);
        records = new AtomicAccessRecords(record, stored ? record : records.LastStore);
        return record;
    }

    public bool TryGetLastAtomicAccess(
        FieldId fieldId,
        ProcessTrackedObjectId? objectId,
        ProcessThreadId threadId,
        bool storesOnly,
        out AccessRecord record)
    {
        if (TryGetSlots(fieldId, objectId, out var slots) &&
            slots.AtomicByThread != null &&
            slots.AtomicByThread.TryGetValue(threadId, out var records))
        {
            if (!storesOnly)
            {
                record = records.Last;
                return true;
            }

            if (records.LastStore is { } lastStore)
            {
                record = lastStore;
                return true;
            }
        }

        record = default;
        return false;
    }

    private ref AccessSlots GetOrAddSlots(FieldId fieldId, ProcessTrackedObjectId? objectId)
    {
        if (objectId == null)
            return ref CollectionsMarshal.GetValueRefOrAddDefault(_staticFieldSlots, fieldId, out _);

        if (!_instanceFieldSlots.TryGetValue(objectId.Value, out var fields))
        {
            fields = [];
            _instanceFieldSlots[objectId.Value] = fields;
        }

        return ref CollectionsMarshal.GetValueRefOrAddDefault(fields, fieldId, out _);
    }

    private static void UpdateSlots(ref AccessSlots slots, in AccessRecord record, bool trackReadsByThread)
    {
        if (record.AccessType == AccessType.Write)
        {
            slots.LastWrite = record;
            slots.HasWrite = true;
            slots.ReadsByThread = null;
            slots.AtomicByThread = null;
        }
        else if (trackReadsByThread)
        {
            var reads = slots.ReadsByThread ??= [];

            if (reads.Count == 0 &&
                slots is { HasLast: true, Last.AccessType: AccessType.Read } &&
                slots.Last.ProcessThreadId != record.ProcessThreadId)
            {
                reads[slots.Last.ProcessThreadId] = slots.Last;
            }

            reads[record.ProcessThreadId] = record;
        }

        slots.Last = record;
        slots.HasLast = true;
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
