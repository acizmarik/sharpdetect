// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Serialization;

namespace SharpDetect.Core.Communication;

public sealed class EventBatchReader(IRecordedEventParser parser)
{
    private ReadOnlyMemory<byte> _batch;
    private int _offset;
    private bool _exhausted = true;
    public bool HasPendingRecords => !_exhausted;
    
    public void SetBatch(ReadOnlyMemory<byte> batch)
    {
        _batch = batch;
        _offset = 0;
        _exhausted = false;
    }
    
    public void Reset()
    {
        _batch = default;
        _offset = 0;
        _exhausted = true;
    }
    
    public EventBatchReadResult ReadInto(Span<RecordedEvent> destination)
    {
        var count = 0;
        var failedRecords = 0;
        Exception? lastFailure = null;

        while (count < destination.Length && !_exhausted)
        {
            var status = EventBatchProtocol.TryReadRecord(_batch, ref _offset, out var record);
            if (status != EventBatchRecordStatus.Record)
            {
                _exhausted = true;
                return new EventBatchReadResult(
                    count,
                    failedRecords,
                    Corrupted: status == EventBatchRecordStatus.Corrupted,
                    lastFailure);
            }

            try
            {
                destination[count] = parser.Parse(record);
                count++;
            }
            catch (Exception ex)
            {
                failedRecords++;
                lastFailure = ex;
            }
        }

        return new EventBatchReadResult(count, failedRecords, Corrupted: false, lastFailure);
    }
}
