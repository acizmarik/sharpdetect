// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Buffers.Binary;
using SharpDetect.Core.Communication;
using Xunit;

namespace SharpDetect.Core.Tests.Communication;

public class EventBatchProtocolTests
{
    [Fact]
    public void TryReadRecord_ReadsRecordsInOrder()
    {
        var batch = BuildBatch([1, 2, 3], [4, 5], []);

        var offset = 0;
        Assert.Equal(EventBatchRecordStatus.Record, EventBatchProtocol.TryReadRecord(batch, ref offset, out var first));
        Assert.Equal(EventBatchRecordStatus.Record, EventBatchProtocol.TryReadRecord(batch, ref offset, out var second));
        Assert.Equal(EventBatchRecordStatus.Record, EventBatchProtocol.TryReadRecord(batch, ref offset, out var third));
        Assert.Equal(EventBatchRecordStatus.EndOfBatch, EventBatchProtocol.TryReadRecord(batch, ref offset, out _));

        Assert.Equal<byte>([1, 2, 3], first.ToArray());
        Assert.Equal<byte>([4, 5], second.ToArray());
        Assert.Empty(third.ToArray());
        Assert.Equal(batch.Length, offset);
    }

    [Fact]
    public void TryReadRecord_ReportsEndOfEmptyBatch()
    {
        var offset = 0;
        Assert.Equal(
            EventBatchRecordStatus.EndOfBatch,
            EventBatchProtocol.TryReadRecord(ReadOnlyMemory<byte>.Empty, ref offset, out _));
    }

    [Fact]
    public void TryReadRecord_ReportsTruncatedHeaderAsCorrupted()
    {
        var offset = 0;
        Assert.Equal(
            EventBatchRecordStatus.Corrupted,
            EventBatchProtocol.TryReadRecord(new byte[] { 1, 2, 3 }, ref offset, out _));
    }

    [Fact]
    public void TryReadRecord_ReportsSizeOverrunningTheBatchAsCorrupted()
    {
        var batch = new byte[EventBatchProtocol.RecordHeaderSize + 2];
        BinaryPrimitives.WriteInt32LittleEndian(batch, 64);

        var offset = 0;
        Assert.Equal(EventBatchRecordStatus.Corrupted, EventBatchProtocol.TryReadRecord(batch, ref offset, out _));
        Assert.Equal(0, offset);
    }

    [Fact]
    public void TryReadRecord_ReportsNegativeSizeAsCorrupted()
    {
        var batch = new byte[EventBatchProtocol.RecordHeaderSize + 4];
        BinaryPrimitives.WriteInt32LittleEndian(batch, -1);

        var offset = 0;
        Assert.Equal(EventBatchRecordStatus.Corrupted, EventBatchProtocol.TryReadRecord(batch, ref offset, out _));
    }

    [Fact]
    public void TryReadRecord_ReportsOffsetPastTheBatchAsCorrupted()
    {
        var batch = BuildBatch([1]);

        var offset = batch.Length + 1;
        Assert.Equal(EventBatchRecordStatus.Corrupted, EventBatchProtocol.TryReadRecord(batch, ref offset, out _));
    }

    internal static byte[] BuildBatch(params byte[][] records)
    {
        var size = records.Sum(record => EventBatchProtocol.RecordHeaderSize + record.Length);
        var batch = new byte[size];

        var offset = 0;
        foreach (var record in records)
        {
            BinaryPrimitives.WriteInt32LittleEndian(batch.AsSpan(offset), record.Length);
            offset += EventBatchProtocol.RecordHeaderSize;
            record.CopyTo(batch.AsSpan(offset));
            offset += record.Length;
        }

        return batch;
    }
}
