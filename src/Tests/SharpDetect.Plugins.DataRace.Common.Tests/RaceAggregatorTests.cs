// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using dnlib.DotNet;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using Xunit;

namespace SharpDetect.Plugins.DataRace.Common.Tests;

public class RaceAggregatorTests
{
    private const int WriteMethodToken = 0x06000010;
    private const int ReadMethodToken = 0x06000011;

    private readonly ModuleDefUser _module;
    private readonly FieldId _field;
    private readonly FieldId _otherField;

    public RaceAggregatorTests()
    {
        _module = new ModuleDefUser("TestModule");
        var assembly = new AssemblyDefUser("TestAssembly", new Version(1, 0, 0, 0));
        assembly.Modules.Add(_module);
        _field = CreateField("Counter");
        _otherField = CreateField("Other");
    }

    [Fact]
    public void Aggregate_SameSitesObservedRepeatedly_FoldsIntoOneRace()
    {
        var races = Enumerable.Range(0, 12)
            .Select(_ => CreateRace(_field, objectId: 1))
            .ToArray();

        var groups = RaceAggregator.Aggregate(races);

        var group = Assert.Single(groups);
        Assert.Equal(12, group.OccurrenceCount);
    }

    [Fact]
    public void Aggregate_OccurrenceCounts_SumToInputCount()
    {
        DataRaceInfo[] races =
        [
            CreateRace(_field, objectId: 1),
            CreateRace(_field, objectId: 2),
            CreateRace(_otherField, objectId: 3),
            CreateRace(_field, objectId: 1, laterAccessType: AccessType.Read)
        ];

        var groups = RaceAggregator.Aggregate(races);

        Assert.Equal(races.Length, groups.Sum(group => group.OccurrenceCount));
    }

    [Fact]
    public void Aggregate_SameRaceObservedFromEitherDirection_FoldsOntoOneKey()
    {
        var forward = CreateRace(
            _field,
            objectId: 1,
            earlierToken: WriteMethodToken,
            laterToken: ReadMethodToken,
            earlierAccessType: AccessType.Write,
            laterAccessType: AccessType.Read);
        var reversed = CreateRace(
            _field,
            objectId: 1,
            earlierToken: ReadMethodToken,
            laterToken: WriteMethodToken,
            earlierAccessType: AccessType.Read,
            laterAccessType: AccessType.Write);

        var groups = RaceAggregator.Aggregate([forward, reversed]);

        var group = Assert.Single(groups);
        Assert.Equal(2, group.OccurrenceCount);
        Assert.True(group.BothOrderingsObserved);
    }

    [Fact]
    public void Aggregate_RaceAlwaysObservedInOneDirection_DoesNotClaimBothOrderings()
    {
        var groups = RaceAggregator.Aggregate(
        [
            CreateRace(_field, objectId: 1),
            CreateRace(_field, objectId: 2),
            CreateRace(_field, objectId: 3)
        ]);

        var group = Assert.Single(groups);
        Assert.Equal(3, group.OccurrenceCount);
        Assert.False(group.BothOrderingsObserved);
    }

    [Fact]
    public void Aggregate_ReadWriteAndWriteWriteAtSameSites_StayDistinct()
    {
        var readWrite = CreateRace(
            _field,
            objectId: 1,
            earlierAccessType: AccessType.Read,
            laterAccessType: AccessType.Write);
        var writeWrite = CreateRace(
            _field,
            objectId: 1,
            earlierAccessType: AccessType.Write,
            laterAccessType: AccessType.Write);

        var groups = RaceAggregator.Aggregate([readWrite, writeWrite]);

        Assert.Equal(2, groups.Count);
    }

    [Fact]
    public void Aggregate_DifferentCallPaths_StayDistinct()
    {
        var viaFirstCaller = CreateRace(_field, objectId: 1, callerToken: 0x06000100);
        var viaSecondCaller = CreateRace(_field, objectId: 1, callerToken: 0x06000200);

        var groups = RaceAggregator.Aggregate([viaFirstCaller, viaSecondCaller]);

        Assert.Equal(2, groups.Count);
    }

    [Fact]
    public void Aggregate_SameTokenAndOffsetInDifferentModules_StaysDistinct()
    {
        var inFirstModule = CreateRace(_field, objectId: 1, earlierModuleId: 1);
        var inSecondModule = CreateRace(_field, objectId: 1, earlierModuleId: 2);

        var groups = RaceAggregator.Aggregate([inFirstModule, inSecondModule]);

        Assert.Equal(2, groups.Count);
    }

    [Fact]
    public void Aggregate_DifferentFields_StayDistinct()
    {
        var groups = RaceAggregator.Aggregate(
        [
            CreateRace(_field, objectId: 1),
            CreateRace(_otherField, objectId: 1)
        ]);

        Assert.Equal(2, groups.Count);
    }

    [Fact]
    public void Aggregate_AccumulatesDistinctObjectIds()
    {
        var groups = RaceAggregator.Aggregate(
        [
            CreateRace(_field, objectId: 7),
            CreateRace(_field, objectId: 8),
            CreateRace(_field, objectId: 7)
        ]);

        var group = Assert.Single(groups);
        Assert.Equal(3, group.OccurrenceCount);
        Assert.Equal(2, group.ObjectIds.Length);
    }

    [Fact]
    public void Aggregate_StaticField_HasNoObjectIds()
    {
        var groups = RaceAggregator.Aggregate([CreateRace(_field, objectId: null)]);

        var group = Assert.Single(groups);
        Assert.Empty(group.ObjectIds);
    }

    [Fact]
    public void Aggregate_RepresentativeIsTheFirstObservedOccurrence()
    {
        var baseTime = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);
        var groups = RaceAggregator.Aggregate(
        [
            CreateRace(_field, objectId: 1, timestamp: baseTime),
            CreateRace(_field, objectId: 1, timestamp: baseTime.AddSeconds(10)),
            CreateRace(_field, objectId: 1, timestamp: baseTime.AddSeconds(30))
        ]);

        var group = Assert.Single(groups);
        Assert.Equal(baseTime, group.Representative.Timestamp);
    }

    private FieldId CreateField(string name)
    {
        var type = new TypeDefUser("TestNamespace", $"Holder{name}", _module.CorLibTypes.Object.TypeDefOrRef);
        _module.Types.Add(type);

        var field = new FieldDefUser(
            name,
            new FieldSig(_module.CorLibTypes.Int32),
            FieldAttributes.Public);
        type.Fields.Add(field);

        return new FieldId(1, new ModuleId(1), new MdToken(0x04000001), field);
    }

    private static DataRaceInfo CreateRace(
        FieldId field,
        int? objectId,
        int earlierToken = WriteMethodToken,
        int laterToken = ReadMethodToken,
        AccessType earlierAccessType = AccessType.Write,
        AccessType laterAccessType = AccessType.Read,
        int? callerToken = null,
        DateTime? timestamp = null,
        nuint earlierModuleId = 1,
        nuint laterModuleId = 1)
    {
        return new DataRaceInfo(
            ProcessId: 1,
            FieldId: field,
            ObjectId: objectId is { } id
                ? new ProcessTrackedObjectId(1, new TrackedObjectId((nuint)id))
                : null,
            CurrentAccess: CreateAccess(2, laterModuleId, laterToken, laterAccessType, callerToken),
            LastAccess: CreateAccess(1, earlierModuleId, earlierToken, earlierAccessType, callerToken),
            Timestamp: timestamp ?? DateTime.UnixEpoch);
    }

    private static AccessInfo CreateAccess(
        nuint threadId,
        nuint moduleId,
        int methodToken,
        AccessType accessType,
        int? callerToken)
    {
        var top = new CapturedStackFrame(new ModuleId(moduleId), new MdMethodDef(methodToken));
        var stack = callerToken is { } caller
            ? new CapturedStackTrace(top, CreateDeeperFramesBlob(methodToken, caller))
            : new CapturedStackTrace(top);

        return new AccessInfo(
            ProcessThreadId: new ProcessThreadId(1, new ThreadId(threadId)),
            ThreadName: $"T{threadId}",
            MethodOffset: 0,
            AccessType: accessType,
            Stack: stack);
    }
    
    private static byte[] CreateDeeperFramesBlob(params int[] methodTokens)
    {
        const int entrySize = sizeof(ulong) + sizeof(uint);
        var blob = new byte[methodTokens.Length * entrySize];
        var span = blob.AsSpan();

        for (var i = 0; i < methodTokens.Length; i++)
        {
            var offset = i * entrySize;
            MemoryMarshal.Write(span[offset..], (ulong)1);
            MemoryMarshal.Write(span[(offset + sizeof(ulong))..], (uint)methodTokens[i]);
        }

        return blob;
    }
}
