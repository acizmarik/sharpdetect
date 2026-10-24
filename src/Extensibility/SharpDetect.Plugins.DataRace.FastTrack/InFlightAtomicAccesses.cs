// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;
using SharpDetect.Plugins.DataRace.Common;

namespace SharpDetect.Plugins.DataRace.FastTrack;

internal sealed class InFlightAtomicAccesses
{
    private readonly Dictionary<ProcessThreadId, InFlightAtomicAccess> _operations = [];
    private int _activeCount;

    public InFlightAtomicAccess Begin(ProcessThreadId threadId, Epoch release)
    {
        if (!_operations.TryGetValue(threadId, out var operation))
        {
            operation = new InFlightAtomicAccess();
            _operations[threadId] = operation;
        }

        if (!operation.IsActive)
            _activeCount++;

        operation.Reset(release);
        return operation;
    }

    public bool TryComplete(ProcessThreadId threadId, out InFlightAtomicAccess? operation)
    {
        if (!_operations.TryGetValue(threadId, out operation) || !operation.IsActive)
        {
            operation = null;
            return false;
        }

        operation.IsActive = false;
        _activeCount--;
        return true;
    }

    public void NotePlainAccess(ProcessThreadId threadId, VectorClock threadVc, Epoch epoch, in AccessRecord record, bool isWrite)
    {
        if (_activeCount == 0)
            return;

        foreach (var (owner, operation) in _operations)
        {
            if (operation.IsActive && owner != threadId)
                operation.NoteAccess(threadId, new PlainAccess(epoch, record), isWrite, observed: operation.Release.HappensBefore(threadVc));
        }
    }
}

internal readonly record struct PlainAccess(Epoch Epoch, AccessRecord Record);

internal sealed class InFlightAtomicAccess
{
    private Dictionary<ProcessThreadId, PlainAccess>? _unorderedWrites;
    private Dictionary<ProcessThreadId, PlainAccess>? _unorderedReads;
    private Dictionary<ProcessThreadId, PlainAccess>? _observingWrites;

    public bool IsActive { get; set; }
    public Epoch Release { get; private set; }

    public PlainAccess? WriteBefore { get; set; }
    public Epoch ReadEpochBefore { get; set; }
    public VectorClock? ReadClockBefore { get; set; }
    public Dictionary<ProcessThreadId, AccessRecord>? ReadsBefore { get; private set; }

    public void Reset(Epoch release)
    {
        IsActive = true;
        Release = release;
        WriteBefore = null;
        ReadEpochBefore = Epoch.None;
        ReadClockBefore = null;
        ReadsBefore?.Clear();
        _unorderedWrites?.Clear();
        _unorderedReads?.Clear();
        _observingWrites?.Clear();
    }

    public Dictionary<ProcessThreadId, AccessRecord> GetOrCreateReadsBefore() => ReadsBefore ??= [];

    public void NoteAccess(ProcessThreadId threadId, in PlainAccess access, bool isWrite, bool observed)
    {
        // A plain read never races with an operation that turns out to store nothing
        if (observed && !isWrite)
            return;

        var accesses = observed
            ? _observingWrites ??= []
            : isWrite
                ? _unorderedWrites ??= []
                : _unorderedReads ??= [];
        accesses[threadId] = access;
    }

    public AccessRecord? FindUnorderedWrite(VectorClock operationVc) => FindUnordered(_unorderedWrites, operationVc);
    public AccessRecord? FindObservingWrite(VectorClock operationVc) => FindUnordered(_observingWrites, operationVc);

    public AccessRecord? FindUnorderedRead(VectorClock operationVc) => FindUnordered(_unorderedReads, operationVc);

    private static AccessRecord? FindUnordered(Dictionary<ProcessThreadId, PlainAccess>? accesses, VectorClock operationVc)
    {
        if (accesses == null)
            return null;

        foreach (var access in accesses.Values)
        {
            if (!access.Epoch.HappensBefore(operationVc))
                return access.Record;
        }

        return null;
    }
}
