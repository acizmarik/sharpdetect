// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.PluginHost.Tests;

internal static class SyncEventBuilder
{
    public const uint Pid = 42;
    private static readonly ModuleId Module = new(1);
    private static readonly MdMethodDef Method = new(0x06000001);

    public static RecordedEventMetadata Meta(uint tid)
        => new(Pid, new ThreadId(tid));

    public static RecordedEvent EnterWithTarget(uint tid, RecordedEventType type, nuint targetObjectId)
    {
        var args = new byte[Unsafe.SizeOf<nuint>()];
        MemoryMarshal.Write(args, in targetObjectId);
        return new RecordedEvent(Meta(tid), new MethodEnterWithArgumentsRecordedEvent(
            ModuleId: Module,
            MethodToken: Method,
            Interpretation: (ushort)type,
            ArgumentValues: args,
            ArgumentInfos: []));
    }

    public static RecordedEvent EnterWithTargetAndCount(
        uint tid,
        RecordedEventType type,
        nuint targetObjectId,
        int count)
    {
        var args = new byte[Unsafe.SizeOf<nuint>() + sizeof(int)];
        MemoryMarshal.Write(args, in targetObjectId);
        MemoryMarshal.Write(args.AsSpan()[Unsafe.SizeOf<nuint>()..], in count);
        return new RecordedEvent(Meta(tid), new MethodEnterWithArgumentsRecordedEvent(
            ModuleId: Module,
            MethodToken: Method,
            Interpretation: (ushort)type,
            ArgumentValues: args,
            ArgumentInfos: []));
    }

    public static RecordedEvent EnterWithTargetCountAndCapacity(
        uint tid,
        RecordedEventType type,
        nuint targetObjectId,
        int count,
        int capacity)
    {
        var args = new byte[Unsafe.SizeOf<nuint>() + sizeof(int) + sizeof(int)];
        MemoryMarshal.Write(args, in targetObjectId);
        MemoryMarshal.Write(args.AsSpan()[Unsafe.SizeOf<nuint>()..], in count);
        MemoryMarshal.Write(args.AsSpan()[(Unsafe.SizeOf<nuint>() + sizeof(int))..], in capacity);
        return new RecordedEvent(Meta(tid), new MethodEnterWithArgumentsRecordedEvent(
            ModuleId: Module,
            MethodToken: Method,
            Interpretation: (ushort)type,
            ArgumentValues: args,
            ArgumentInfos: []));
    }

    public static RecordedEvent Exit(uint tid, RecordedEventType type)
        => new(Meta(tid), new MethodExitRecordedEvent(
            ModuleId: Module,
            MethodToken: Method,
            Interpretation: (ushort)type));

    public static RecordedEvent ExitWithSuccess(uint tid, RecordedEventType type, bool success)
    {
        var ret = new byte[1];
        MemoryMarshal.Write(ret, in success);
        return new RecordedEvent(Meta(tid), new MethodExitWithArgumentsRecordedEvent(
            ModuleId: Module,
            MethodToken: Method,
            Interpretation: (ushort)type,
            ReturnValue: ret,
            ByRefArgumentValues: [],
            ByRefArgumentInfos: []));
    }

    public static RecordedEvent ExitWithByRefSuccess(uint tid, RecordedEventType type, bool success)
    {
        var byRef = new byte[1];
        MemoryMarshal.Write(byRef, in success);
        return new RecordedEvent(Meta(tid), new MethodExitWithArgumentsRecordedEvent(
            ModuleId: Module,
            MethodToken: Method,
            Interpretation: (ushort)type,
            ReturnValue: [],
            ByRefArgumentValues: byRef,
            ByRefArgumentInfos: []));
    }

    public static RecordedEvent FieldRead(uint tid)
        => new(Meta(tid), new MethodEnterWithArgumentsRecordedEvent(
            ModuleId: Module,
            MethodToken: Method,
            Interpretation: (ushort)RecordedEventType.InstanceFieldRead,
            ArgumentValues: [],
            ArgumentInfos: []));

    public static RecordedEvent FieldAccessInstrumentation(uint tid, ulong instrumentationId)
        => new(Meta(tid), new FieldAccessInstrumentationRecordedEvent(
            ModuleId: Module,
            MethodToken: Method,
            MethodOffset: 0,
            FieldToken: new MdToken(0x04000001),
            InstrumentationId: instrumentationId,
            IsVolatile: false));

    public static RecordedEvent ProfilerDestroy(uint tid)
        => new(Meta(tid), new ProfilerDestroyRecordedEvent());

    public static RecordedEvent ThreadDestroy(uint tid)
        => new(Meta(tid), new ThreadDestroyRecordedEvent(new ThreadId(tid)));

    public static RecordedEvent ThreadDestroy(uint destroyedTid, uint emitterTid)
        => new(Meta(emitterTid), new ThreadDestroyRecordedEvent(new ThreadId(destroyedTid)));

    public static RecordedEvent GarbageCollected(uint emitterTid, params nuint[] removedObjects)
    {
        var ids = new TrackedObjectId[removedObjects.Length];
        for (var i = 0; i < removedObjects.Length; i++)
            ids[i] = new TrackedObjectId(removedObjects[i]);

        return new RecordedEvent(Meta(emitterTid), new GarbageCollectedTrackedObjectsRecordedEvent(ids));
    }
}
