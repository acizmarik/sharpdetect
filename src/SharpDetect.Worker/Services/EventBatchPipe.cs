// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using SharpDetect.Core.Events;

namespace SharpDetect.Worker.Services;

/// <summary>
/// Single-producer/single-consumer handoff between the event receiver thread and the event processing thread
/// </summary>
internal sealed class EventBatchPipe : IDisposable
{
    private readonly int _batchSize;
    private readonly BlockingCollection<EventBatch> _batches;
    private readonly ConcurrentQueue<RecordedEvent[]> _spareBuffers;
    private RecordedEvent[]? _writeBuffer;
    private int _writeCount;

    public EventBatchPipe(int batchSize, int maxPendingBatches)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPendingBatches, 1);

        _batchSize = batchSize;
        _batches = new BlockingCollection<EventBatch>(maxPendingBatches);
        _spareBuffers = new ConcurrentQueue<RecordedEvent[]>();
    }
    
    public Span<RecordedEvent> GetWriteSpan()
    {
        var buffer = _writeBuffer ??= RentBuffer();
        return buffer.AsSpan(_writeCount);
    }
    
    public void Advance(int count, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, _batchSize - _writeCount);

        _writeCount += count;
        if (_writeCount == _batchSize)
            PublishCurrentBatch(cancellationToken);
    }
    
    public void Flush(CancellationToken cancellationToken)
    {
        if (_writeCount > 0)
            PublishCurrentBatch(cancellationToken);
    }
    
    public void Complete(CancellationToken cancellationToken)
    {
        try
        {
            Flush(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // The consumer has already stopped draining
        }
        finally
        {
            _batches.CompleteAdding();
        }
    }
    
    public bool TryTakeBatch(out EventBatch batch, CancellationToken cancellationToken)
        => _batches.TryTake(out batch, Timeout.Infinite, cancellationToken);
    
    public void Recycle(EventBatch batch)
    {
        Array.Clear(batch.Buffer);
        _spareBuffers.Enqueue(batch.Buffer);
    }
    
    public void Dispose()
        => _batches.Dispose();

    private void PublishCurrentBatch(CancellationToken cancellationToken)
    {
        var batch = new EventBatch(_writeBuffer!, _writeCount);
        _writeBuffer = null;
        _writeCount = 0;
        _batches.Add(batch, cancellationToken);
    }

    private RecordedEvent[] RentBuffer()
        => _spareBuffers.TryDequeue(out var buffer) ? buffer : new RecordedEvent[_batchSize];
}
