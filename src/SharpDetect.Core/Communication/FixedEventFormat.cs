// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Core.Communication;

public static class FixedEventFormat
{
    public const byte MsgPackFormat = 0;

    /// <summary>
    /// [u64 threadId][u64 moduleId][u32 methodToken][u16 interpretation]
    /// The process id and command id are not on the wire
    /// </summary>
    public const int HeaderSize = 22;

    private const int BlobLengthSize = sizeof(uint);
    private const uint AbsentBlob = 0xFFFFFFFFu;

    public static bool TryRead(
        byte format,
        ReadOnlySpan<byte> payload,
        out ThreadId threadId,
        [NotNullWhen(true)] out IRecordedEventArgs? eventArgs)
    {
        threadId = default;
        eventArgs = null;

        if (payload.Length < HeaderSize)
            return false;

        var rawThreadId = BinaryPrimitives.ReadUInt64LittleEndian(payload);
        var moduleId = new ModuleId((nuint)BinaryPrimitives.ReadUInt64LittleEndian(payload[8..]));
        var methodToken = new MdMethodDef(BinaryPrimitives.ReadInt32LittleEndian(payload[16..]));
        var interpretation = BinaryPrimitives.ReadUInt16LittleEndian(payload[20..]);
        var body = payload[HeaderSize..];

        switch ((RecordedEventType)format)
        {
            case RecordedEventType.MethodEnter:
            {
                if (body.Length != 0)
                    return false;
                
                eventArgs = new MethodEnterRecordedEvent(moduleId, methodToken, interpretation);
                break;
            }

            case RecordedEventType.MethodExit:
            {
                if (body.Length != 0)
                    return false;
                
                eventArgs = new MethodExitRecordedEvent(moduleId, methodToken, interpretation);
                break;
            }
            
            case RecordedEventType.MethodEnterWithArguments:
            {
                if (!TryReadBlobs(body, out var argumentValues, out var argumentInfos, out var stackFrames))
                    return false;

                eventArgs = new MethodEnterWithArgumentsRecordedEvent(
                    moduleId, methodToken, interpretation, argumentValues, argumentInfos, stackFrames);
                break;
            }

            case RecordedEventType.MethodExitWithArguments:
            {
                if (!TryReadBlobs(body, out var returnValue, out var byRefValues, out var byRefInfos)
                    || byRefInfos is null)
                {
                    return false;
                }

                eventArgs = new MethodExitWithArgumentsRecordedEvent(
                    moduleId, methodToken, interpretation, returnValue, byRefValues, byRefInfos);
                break;
            }

            default:
                return false;
        }

        threadId = new ThreadId((nuint)rawThreadId);
        return true;
    }
    
    private static bool TryReadBlobs(
        ReadOnlySpan<byte> body,
        [NotNullWhen(true)] out byte[]? first,
        [NotNullWhen(true)] out byte[]? second,
        out byte[]? third)
    {
        first = null;
        second = null;
        third = null;

        const int lengthsSize = 3 * BlobLengthSize;
        if (body.Length < lengthsSize)
            return false;

        var firstLength = BinaryPrimitives.ReadUInt32LittleEndian(body);
        var secondLength = BinaryPrimitives.ReadUInt32LittleEndian(body[BlobLengthSize..]);
        var thirdLength = BinaryPrimitives.ReadUInt32LittleEndian(body[(2 * BlobLengthSize)..]);
        var thirdIsAbsent = thirdLength == AbsentBlob;
        if (thirdIsAbsent)
            thirdLength = 0;

        var blobs = body[lengthsSize..];
        var declared = (long)firstLength + secondLength + thirdLength;
        if (declared != blobs.Length)
            return false;

        first = [.. blobs[..(int)firstLength]];
        second = [.. blobs.Slice((int)firstLength, (int)secondLength)];
        third = thirdIsAbsent ? null : [.. blobs.Slice((int)(firstLength + secondLength), (int)thirdLength)];
        return true;
    }
}
