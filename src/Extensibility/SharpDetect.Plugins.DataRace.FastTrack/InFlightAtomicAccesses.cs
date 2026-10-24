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

    public void NotePlainWrite(ProcessThreadId threadId, VectorClock threadVc, Epoch epoch, in AccessRecord record)
    {
        if (_activeCount == 0)
            return;

        foreach (var (owner, operation) in _operations)
        {
            if (operation.IsActive && owner != threadId)
                operation.NoteWrite(threadId, new PlainAccess(epoch, record), observed: operation.Release.HappensBefore(threadVc));
        }
    }
}

internal readonly record struct PlainAccess(Epoch Epoch, AccessRecord Record);

internal sealed class InFlightAtomicAccess
{
    private Dictionary<ProcessThreadId, PlainAccess>? _unorderedWrites;
    private Dictionary<ProcessThreadId, PlainAccess>? _observingWrites;

    public bool IsActive { get; set; }
    public Epoch Release { get; private set; }
    public PlainAccess? WriteBefore { get; set; }

    public void Reset(Epoch release)
    {
        IsActive = true;
        Release = release;
        WriteBefore = null;
        _unorderedWrites?.Clear();
        _observingWrites?.Clear();
    }

    public void NoteWrite(ProcessThreadId threadId, in PlainAccess access, bool observed)
    {
        var accesses = observed
            ? _observingWrites ??= []
            : _unorderedWrites ??= [];
        accesses[threadId] = access;
    }

    public AccessRecord? FindUnorderedWrite(VectorClock operationVc) => FindUnordered(_unorderedWrites, operationVc);
    public AccessRecord? FindObservingWrite(VectorClock operationVc) => FindUnordered(_observingWrites, operationVc);

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
