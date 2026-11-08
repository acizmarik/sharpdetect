// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.Common;

public sealed class AccessTracker(Func<ProcessThreadId, string?> threadNameResolver)
{
    // Last accesses of one memory location; owned by the location's shadow state
    public struct AccessSlots
    {
        internal AccessRecord Last;
        internal AccessRecord LastWrite;
        internal bool HasLast;
        internal bool HasWrite;
        internal Dictionary<ProcessThreadId, AccessRecord>? ReadsByThread;
        internal Dictionary<ProcessThreadId, AtomicAccessRecords>? AtomicByThread;
    }

    internal readonly record struct AtomicAccessRecords(AccessRecord Last, AccessRecord? LastStore);

    public static bool TryGetLastAccess(ref readonly AccessSlots slots, out AccessRecord record)
    {
        if (slots.HasLast)
        {
            record = slots.Last;
            return true;
        }

        record = default;
        return false;
    }

    public static bool TryGetLastWriteAccess(ref readonly AccessSlots slots, out AccessRecord record)
    {
        if (slots.HasWrite)
        {
            record = slots.LastWrite;
            return true;
        }

        record = default;
        return false;
    }

    public static bool TryGetLastReadAccess(
        ref readonly AccessSlots slots,
        ProcessThreadId readerThreadId,
        out AccessRecord record)
    {
        if (slots.ReadsByThread != null && slots.ReadsByThread.TryGetValue(readerThreadId, out record))
            return true;

        if (slots is { HasLast: true, Last.AccessType: AccessType.Read } && slots.Last.ProcessThreadId == readerThreadId)
        {
            record = slots.Last;
            return true;
        }

        record = default;
        return false;
    }

    public static void CopyLastReadAccesses(
        ref readonly AccessSlots slots,
        Dictionary<ProcessThreadId, AccessRecord> destination)
    {
        if (slots.ReadsByThread != null)
        {
            foreach (var (threadId, record) in slots.ReadsByThread)
                destination[threadId] = record;
        }
        else if (slots is { HasLast: true, Last.AccessType: AccessType.Read })
        {
            destination[slots.Last.ProcessThreadId] = slots.Last;
        }
    }

    public static AccessRecord RecordAccess(
        ref AccessSlots slots,
        ProcessThreadId threadId,
        uint methodOffset,
        AccessType accessType,
        CapturedStackTrace stack,
        bool trackReadsByThread = false)
    {
        var record = new AccessRecord(threadId, methodOffset, accessType, stack);
        UpdateSlots(ref slots, record, trackReadsByThread);
        return record;
    }

    public static AccessRecord RecordAtomicAccess(
        ref AccessSlots slots,
        ProcessThreadId threadId,
        uint methodOffset,
        bool stored,
        CapturedStackTrace stack)
    {
        var record = new AccessRecord(threadId, methodOffset, stored ? AccessType.AtomicWrite : AccessType.AtomicRead, stack);
        var atomicByThread = slots.AtomicByThread ??= [];
        ref var records = ref CollectionsMarshal.GetValueRefOrAddDefault(atomicByThread, threadId, out _);
        records = new AtomicAccessRecords(record, stored ? record : records.LastStore);
        return record;
    }

    public static bool TryGetLastAtomicAccess(
        ref readonly AccessSlots slots,
        ProcessThreadId threadId,
        bool storesOnly,
        out AccessRecord record)
    {
        if (slots.AtomicByThread != null &&
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

    public AccessInfo Materialize(in AccessRecord record)
    {
        return new AccessInfo(
            record.ProcessThreadId,
            threadNameResolver(record.ProcessThreadId),
            record.MethodOffset,
            record.AccessType,
            record.Stack);
    }
}
