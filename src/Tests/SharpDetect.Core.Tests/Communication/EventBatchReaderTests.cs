// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Buffers.Binary;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Serialization;
using Xunit;

namespace SharpDetect.Core.Tests.Communication;

public class EventBatchReaderTests
{
    private sealed class StubParser : IRecordedEventParser
    {
        public const uint UnparsableMarker = uint.MaxValue;

        public RecordedEvent Parse(ReadOnlyMemory<byte> input)
        {
            var pid = BinaryPrimitives.ReadUInt32LittleEndian(input.Span);
            return pid != UnparsableMarker
                ? new RecordedEvent(new RecordedEventMetadata(pid, new ThreadId(0)), new ProfilerDestroyRecordedEvent())
                : throw new InvalidOperationException("Unparsable record.");
        }
    }

    private static byte[] Record(uint value)
    {
        var record = new byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(record, value);
        return record;
    }

    private static uint[] PidsOf(ReadOnlySpan<RecordedEvent> events, int count)
    {
        var pids = new uint[count];
        for (var index = 0; index < count; index++)
            pids[index] = events[index].Metadata.Pid;

        return pids;
    }

    [Fact]
    public void ReadInto_ReadsWholeBatchWhenDestinationIsLargeEnough()
    {
        var reader = new EventBatchReader(new StubParser());
        reader.SetBatch(EventBatchProtocolTests.BuildBatch(Record(1), Record(2), Record(3)));

        var destination = new RecordedEvent[8];
        var result = reader.ReadInto(destination);

        Assert.Equal(3, result.Count);
        Assert.Equal(0, result.FailedRecords);
        Assert.False(result.Corrupted);
        Assert.False(reader.HasPendingRecords);
        Assert.Equal<uint>([1, 2, 3], PidsOf(destination, result.Count));
    }

    [Fact]
    public void ReadInto_KeepsLeftoversForTheNextCall()
    {
        var reader = new EventBatchReader(new StubParser());
        reader.SetBatch(EventBatchProtocolTests.BuildBatch(Record(1), Record(2), Record(3), Record(4)));

        var destination = new RecordedEvent[2];

        var first = reader.ReadInto(destination);
        Assert.Equal(2, first.Count);
        Assert.True(reader.HasPendingRecords);
        Assert.Equal<uint>([1, 2], PidsOf(destination, first.Count));

        var second = reader.ReadInto(destination);
        Assert.Equal(2, second.Count);
        Assert.Equal<uint>([3, 4], PidsOf(destination, second.Count));

        Assert.True(reader.HasPendingRecords);
        var third = reader.ReadInto(destination);
        Assert.Equal(0, third.Count);
        Assert.False(reader.HasPendingRecords);
    }

    [Fact]
    public void ReadInto_SkipsUnparsableRecordsAndKeepsGoing()
    {
        var reader = new EventBatchReader(new StubParser());
        reader.SetBatch(EventBatchProtocolTests.BuildBatch(
            Record(1),
            Record(StubParser.UnparsableMarker),
            Record(2)));

        var destination = new RecordedEvent[8];
        var result = reader.ReadInto(destination);

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result.FailedRecords);
        Assert.IsType<InvalidOperationException>(result.LastFailure);
        Assert.False(result.Corrupted);
        Assert.False(reader.HasPendingRecords);
        Assert.Equal<uint>([1, 2], PidsOf(destination, result.Count));
    }

    [Fact]
    public void ReadInto_ReportsAnUnparsableBatchWithoutLosingItsPosition()
    {
        var reader = new EventBatchReader(new StubParser());
        reader.SetBatch(EventBatchProtocolTests.BuildBatch(
            Record(StubParser.UnparsableMarker),
            Record(StubParser.UnparsableMarker)));

        var destination = new RecordedEvent[8];
        var result = reader.ReadInto(destination);

        Assert.Equal(0, result.Count);
        Assert.Equal(2, result.FailedRecords);
        Assert.False(reader.HasPendingRecords);
    }

    [Fact]
    public void ReadInto_AbandonsTheBatchOnBrokenFraming()
    {
        var reader = new EventBatchReader(new StubParser());
        var batch = EventBatchProtocolTests.BuildBatch(Record(1), Record(2));
        BinaryPrimitives.WriteInt32LittleEndian(batch.AsSpan(EventBatchProtocol.RecordHeaderSize + sizeof(uint)), 1024);
        reader.SetBatch(batch);

        var destination = new RecordedEvent[8];
        var result = reader.ReadInto(destination);

        Assert.Equal(1, result.Count);
        Assert.True(result.Corrupted);
        Assert.False(reader.HasPendingRecords);
        Assert.Equal<uint>([1], PidsOf(destination, result.Count));
    }

    [Fact]
    public void ReadInto_ReturnsNothingWhenNoBatchIsSet()
    {
        var reader = new EventBatchReader(new StubParser());

        var result = reader.ReadInto(new RecordedEvent[8]);

        Assert.Equal(0, result.Count);
        Assert.False(reader.HasPendingRecords);
    }

    [Fact]
    public void Reset_AbandonsThePendingBatch()
    {
        var reader = new EventBatchReader(new StubParser());
        reader.SetBatch(EventBatchProtocolTests.BuildBatch(Record(1), Record(2)));

        Assert.Equal(1, reader.ReadInto(new RecordedEvent[1]).Count);
        Assert.True(reader.HasPendingRecords);

        reader.Reset();

        Assert.False(reader.HasPendingRecords);
        Assert.Equal(0, reader.ReadInto(new RecordedEvent[8]).Count);
    }
}
