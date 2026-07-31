// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Buffers;
using MessagePack;
using MessagePack.Formatters;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Serialization.Formatters;

internal sealed class RecordedEventFormatter(IFormatterResolver resolver)
{
    private const int EnvelopeMemberCount = 2;
    private const int MetadataMemberCount = 3;
    private const int UnionMemberCount = 2;
    private const int MethodCallMemberCount = 3;
    private const int MethodCallWithArgumentsMemberCount = 6;

    private readonly IMessagePackFormatter<RecordedEvent> _envelopeFormatter = resolver.GetFormatterWithVerify<RecordedEvent>();

    public RecordedEvent Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var rewound = reader;
        if (TryDeserializeMethodEvent(ref reader, out var recordedEvent))
            return recordedEvent;

        reader = rewound;
        return _envelopeFormatter.Deserialize(ref reader, options);
    }

    private static bool TryDeserializeMethodEvent(ref MessagePackReader reader, out RecordedEvent recordedEvent)
    {
        recordedEvent = null!;

        if (reader.NextMessagePackType != MessagePackType.Array || reader.ReadArrayHeader() != EnvelopeMemberCount)
            return false;

        if (reader.NextMessagePackType != MessagePackType.Array || reader.ReadArrayHeader() != MetadataMemberCount)
            return false;

        var pid = reader.ReadUInt32();
        var tid = new ThreadId(new UIntPtr(reader.ReadUInt64()));
        var commandId = reader.TryReadNil() ? null : (ulong?)reader.ReadUInt64();
        var metadata = new RecordedEventMetadata(pid, tid, commandId);

        if (reader.NextMessagePackType != MessagePackType.Array || reader.ReadArrayHeader() != UnionMemberCount)
            return false;

        var eventType = (RecordedEventType)reader.ReadInt32();
        if (reader.NextMessagePackType != MessagePackType.Array)
            return false;

        var memberCount = reader.ReadArrayHeader();
        switch (eventType)
        {
            case RecordedEventType.MethodEnterWithArguments when memberCount == MethodCallWithArgumentsMemberCount:
            {
                var moduleId = ReadModuleId(ref reader);
                var methodToken = ReadMethodToken(ref reader);
                var interpretation = reader.ReadUInt16();
                var argumentValues = ReadPayload(ref reader);
                var argumentInfos = ReadPayload(ref reader);
                var stackFrames = ReadPayload(ref reader);
                recordedEvent = new RecordedEvent(metadata, new MethodEnterWithArgumentsRecordedEvent(
                    moduleId, methodToken, interpretation, argumentValues!, argumentInfos!, stackFrames));
                return true;
            }

            case RecordedEventType.MethodExitWithArguments when memberCount == MethodCallWithArgumentsMemberCount:
            {
                var moduleId = ReadModuleId(ref reader);
                var methodToken = ReadMethodToken(ref reader);
                var interpretation = reader.ReadUInt16();
                var returnValue = ReadPayload(ref reader);
                var byRefArgumentValues = ReadPayload(ref reader);
                var byRefArgumentInfos = ReadPayload(ref reader);
                recordedEvent = new RecordedEvent(metadata, new MethodExitWithArgumentsRecordedEvent(
                    moduleId, methodToken, interpretation, returnValue!, byRefArgumentValues!, byRefArgumentInfos!));
                return true;
            }

            case RecordedEventType.MethodEnter when memberCount == MethodCallMemberCount:
            {
                var moduleId = ReadModuleId(ref reader);
                var methodToken = ReadMethodToken(ref reader);
                var interpretation = reader.ReadUInt16();
                recordedEvent = new RecordedEvent(metadata,
                    new MethodEnterRecordedEvent(moduleId, methodToken, interpretation));
                return true;
            }

            case RecordedEventType.MethodExit when memberCount == MethodCallMemberCount:
            {
                var moduleId = ReadModuleId(ref reader);
                var methodToken = ReadMethodToken(ref reader);
                var interpretation = reader.ReadUInt16();
                recordedEvent = new RecordedEvent(metadata,
                    new MethodExitRecordedEvent(moduleId, methodToken, interpretation));
                return true;
            }

            case RecordedEventType.MethodUnwound when memberCount == MethodCallMemberCount:
            {
                var moduleId = ReadModuleId(ref reader);
                var methodToken = ReadMethodToken(ref reader);
                var interpretation = reader.ReadUInt16();
                recordedEvent = new RecordedEvent(metadata,
                    new MethodUnwoundRecordedEvent(moduleId, methodToken, interpretation));
                return true;
            }

            default:
                return false;
        }
    }

    private static ModuleId ReadModuleId(ref MessagePackReader reader)
    {
        return new ModuleId(new UIntPtr(reader.ReadUInt64()));
    }

    private static MdMethodDef ReadMethodToken(ref MessagePackReader reader)
    {
        return new MdMethodDef(reader.ReadInt32());
    }

    private static byte[]? ReadPayload(ref MessagePackReader reader)
    {
        var payload = reader.ReadBytes();
        if (payload is null)
            return null;

        var sequence = payload.Value;
        return sequence.IsSingleSegment
            ? sequence.FirstSpan.ToArray()
            : sequence.ToArray();
    }
}
