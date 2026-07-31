// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Buffers.Binary;

namespace SharpDetect.Core.Communication;

public static class EventBatchProtocol
{
    public const int RecordHeaderSize = sizeof(int);
    
    public static EventBatchRecordStatus TryReadRecord(
        ReadOnlyMemory<byte> batch,
        ref int offset,
        out byte format,
        out ReadOnlyMemory<byte> payload)
    {
        format = default;
        payload = default;

        var status = TryReadRecord(batch, ref offset, out var record);
        if (status != EventBatchRecordStatus.Record)
            return status;

        if (record.IsEmpty)
            return EventBatchRecordStatus.Corrupted;

        format = record.Span[0];
        payload = record[1..];
        return EventBatchRecordStatus.Record;
    }

    public static EventBatchRecordStatus TryReadRecord(
        ReadOnlyMemory<byte> batch,
        ref int offset,
        out ReadOnlyMemory<byte> record)
    {
        record = default;

        if (offset < 0 || offset > batch.Length)
            return EventBatchRecordStatus.Corrupted;

        if (offset == batch.Length)
            return EventBatchRecordStatus.EndOfBatch;

        var remaining = batch.Length - offset;
        if (remaining < RecordHeaderSize)
            return EventBatchRecordStatus.Corrupted;

        var size = BinaryPrimitives.ReadInt32LittleEndian(batch.Span.Slice(offset, RecordHeaderSize));
        if (size < 0 || size > remaining - RecordHeaderSize)
            return EventBatchRecordStatus.Corrupted;

        record = batch.Slice(offset + RecordHeaderSize, size);
        offset += RecordHeaderSize + size;
        return EventBatchRecordStatus.Record;
    }
}
