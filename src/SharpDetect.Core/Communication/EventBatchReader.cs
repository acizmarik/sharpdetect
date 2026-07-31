// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Serialization;

namespace SharpDetect.Core.Communication;

public sealed class EventBatchReader
{
    private readonly IRecordedEventParser _parser;
    private readonly uint _pid;
    private ReadOnlyMemory<byte> _batch;
    private int _offset;
    private bool _exhausted = true;
    public bool HasPendingRecords => !_exhausted;

    public EventBatchReader(IRecordedEventParser parser, uint pid)
    {
        _parser = parser;
        _pid = pid;
    }
    
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
            var status = EventBatchProtocol.TryReadRecord(_batch, ref _offset, out var format, out var record);
            if (status != EventBatchRecordStatus.Record)
            {
                _exhausted = true;
                return new EventBatchReadResult(
                    count,
                    failedRecords,
                    Corrupted: status == EventBatchRecordStatus.Corrupted,
                    lastFailure);
            }

            if (format != FixedEventFormat.MsgPackFormat)
            {
                if (FixedEventFormat.TryRead(format, record.Span, out var threadId, out var eventArgs))
                {
                    destination[count] = new RecordedEvent(new RecordedEventMetadata(_pid, threadId), eventArgs);
                    count++;
                }
                else
                {
                    failedRecords++;
                    lastFailure = new InvalidDataException(
                        $"Malformed fixed-layout event record (format {format}, {record.Length} bytes).");
                }

                continue;
            }

            try
            {
                destination[count] = _parser.Parse(record);
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
