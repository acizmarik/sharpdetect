// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using Microsoft.Extensions.Logging.Abstractions;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.Plugins.DataRace.Common;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests.Fakes;

internal sealed class DetectorHarness
{
    private readonly TestMetadata _metadata = new();
    private readonly TypeDefUser _fieldsType;
    private readonly TypeDefUser _methodsType;
    private nuint _nextThreadId = 1;
    private nuint _nextObjectId = 1000;
    private int _nextMethod;

    public FastTrackDetector Detector { get; }

    public DetectorHarness()
        : this(FastTrackPluginConfiguration.Default)
    {
    }

    public DetectorHarness(FastTrackPluginConfiguration configuration)
    {
        Detector = new FastTrackDetector(
            configuration,
            _metadata,
            TimeProvider.System,
            NullLogger.Instance,
            _ => null);

        _fieldsType = _metadata.AddType("Fields");
        _methodsType = _metadata.AddType("Methods");
    }

    public ProcessThreadId NewThread()
    {
        var thread = new ProcessThreadId(TestMetadata.ProcessId, new ThreadId(_nextThreadId++));
        Detector.RecordThreadCreated(thread);
        return thread;
    }

    public ProcessTrackedObjectId NewObject()
        => new(TestMetadata.ProcessId, new TrackedObjectId(_nextObjectId++));

    public static ProcessTrackedObjectId ObjectWithId(nuint id)
        => new(TestMetadata.ProcessId, new TrackedObjectId(id));

    public MdToken NewStaticField(string name)
        => _metadata.AddField(_fieldsType, name, isStatic: true);

    public MdToken NewInstanceField(string name)
        => _metadata.AddField(_fieldsType, name, isStatic: false);

    public TypeDefUser NewType(string name)
        => _metadata.AddType(name);

    public TypeDefUser NewGenericType(string name)
    {
        var type = _metadata.AddType(name);
        type.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "T"));
        return type;
    }

    public MdToken NewStaticField(TypeDefUser type, string name, bool isReadOnly = false)
        => _metadata.AddField(type, name, isStatic: true, isReadOnly);

    public MdToken NewInstanceField(TypeDefUser type, string name)
        => _metadata.AddField(type, name, isStatic: false);

    public MdToken NewFieldReference(MdToken field)
        => _metadata.AddFieldReference(field);

    public MdMethodDef NewTypeInitializer(TypeDefUser type)
        => _metadata.AddStaticConstructor(type);

    public void TypeInitializerEntered(ProcessThreadId thread, MdMethodDef typeInitializer)
        => Detector.RecordTypeInitializerEntered(thread, TestMetadata.ModuleId, typeInitializer);

    public void TypeInitialized(ProcessThreadId thread, MdMethodDef typeInitializer)
        => Detector.RecordTypeInitialized(thread, TestMetadata.ModuleId, typeInitializer);

    public void TypeInitializerFailed(ProcessThreadId thread, MdMethodDef typeInitializer)
        => Detector.RecordTypeInitializerFailed(thread, TestMetadata.ModuleId, typeInitializer);

    public CapturedStackTrace NewStack()
        => new(new CapturedStackFrame(TestMetadata.ModuleId, _metadata.AddMethod(_methodsType, $"Step{_nextMethod++}")));

    public DataRaceInfo? Write(ProcessThreadId thread, MdToken field, ProcessTrackedObjectId? instance, nuint declaringTypeHandle = 0)
        => Detector.RecordWrite(thread, methodOffset: 0, field, instance, declaringTypeHandle, NewStack());

    public DataRaceInfo? Read(ProcessThreadId thread, MdToken field, ProcessTrackedObjectId? instance, nuint declaringTypeHandle = 0)
        => Detector.RecordRead(thread, methodOffset: 0, field, instance, declaringTypeHandle, NewStack());

    public bool WriteIsRace(ProcessThreadId thread, MdToken field, ProcessTrackedObjectId? instance)
        => Write(thread, field, instance) is not null;

    public bool ReadIsRace(ProcessThreadId thread, MdToken field, ProcessTrackedObjectId? instance)
        => Read(thread, field, instance) is not null;

    public DataRaceInfo? VolatileWrite(ProcessThreadId thread, MdToken field, ProcessTrackedObjectId? instance, nuint declaringTypeHandle = 0)
        => Detector.RecordVolatileWrite(thread, methodOffset: 0, field, instance, declaringTypeHandle, NewStack());

    public DataRaceInfo? VolatileRead(ProcessThreadId thread, MdToken field, ProcessTrackedObjectId? instance, nuint declaringTypeHandle = 0)
        => Detector.RecordVolatileRead(thread, methodOffset: 0, field, instance, declaringTypeHandle, NewStack());

    public void AtomicRelease(ProcessThreadId thread, MdToken field, ProcessTrackedObjectId? instance)
        => Detector.RecordAtomicRelease(thread, TestMetadata.ModuleId, field, instance, declaringTypeHandle: 0);

    public DataRaceInfo? AtomicAcquire(ProcessThreadId thread, MdToken field, ProcessTrackedObjectId? instance)
        => Detector.RecordAtomicAcquire(thread, methodOffset: 0, field, instance, declaringTypeHandle: 0, NewStack());

    public void CompareExchangeRelease(ProcessThreadId thread, MdToken field, ProcessTrackedObjectId? instance)
        => Detector.RecordCompareExchangeRelease(thread, TestMetadata.ModuleId, field, instance, declaringTypeHandle: 0);

    public DataRaceInfo? CompareExchangeCompleted(ProcessThreadId thread, MdToken field, ProcessTrackedObjectId? instance, bool succeeded)
        => Detector.RecordCompareExchangeCompleted(thread, methodOffset: 0, field, instance, declaringTypeHandle: 0, NewStack(), succeeded);

    public DataRaceInfo? ReadModifyWrite(ProcessThreadId thread, MdToken field, ProcessTrackedObjectId? instance)
    {
        AtomicRelease(thread, field, instance);
        return AtomicAcquire(thread, field, instance);
    }

    public void QueueForFinalization(params ProcessTrackedObjectId[] objects)
    {
        var ids = objects.Select(o => o.ObjectId).ToArray();
        Detector.RecordFinalizationQueuedObjects(TestMetadata.ProcessId, ids);
    }

    public void CollectObjects(params ProcessTrackedObjectId[] objects)
    {
        var ids = objects.Select(o => o.ObjectId).ToArray();
        Detector.RecordGarbageCollectedObjects(TestMetadata.ProcessId, ids);
    }
}
