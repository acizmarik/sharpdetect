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
    {
        Detector = new FastTrackDetector(
            FastTrackPluginConfiguration.Default,
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

    public CapturedStackTrace NewStack()
        => new(new CapturedStackFrame(TestMetadata.ModuleId, _metadata.AddMethod(_methodsType, $"Step{_nextMethod++}")));

    public DataRaceInfo? Write(ProcessThreadId thread, MdToken field, ProcessTrackedObjectId? instance)
        => Detector.RecordWrite(thread, methodOffset: 0, field, instance, NewStack());

    public DataRaceInfo? Read(ProcessThreadId thread, MdToken field, ProcessTrackedObjectId? instance)
        => Detector.RecordRead(thread, methodOffset: 0, field, instance, NewStack());

    public bool WriteIsRace(ProcessThreadId thread, MdToken field, ProcessTrackedObjectId? instance)
        => Write(thread, field, instance) is not null;

    public bool ReadIsRace(ProcessThreadId thread, MdToken field, ProcessTrackedObjectId? instance)
        => Read(thread, field, instance) is not null;

    public void VolatileWrite(ProcessThreadId thread, MdToken field, ProcessTrackedObjectId? instance)
        => Detector.RecordVolatileWrite(thread, TestMetadata.ModuleId, field, instance);

    public void VolatileRead(ProcessThreadId thread, MdToken field, ProcessTrackedObjectId? instance)
        => Detector.RecordVolatileRead(thread, TestMetadata.ModuleId, field, instance);

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
