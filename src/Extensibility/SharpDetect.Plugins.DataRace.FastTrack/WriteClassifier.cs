// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Metadata;
using SharpDetect.Core.Plugins;
using SharpDetect.Plugins.DataRace.Common;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SharpDetect.Plugins.DataRace.FastTrack;

internal sealed class WriteClassifier(IMetadataContext metadataContext, ILogger logger) : ITrackedObjectState
{
    private readonly MethodResolver _methodResolver = new(metadataContext, logger);
    private readonly Dictionary<ProcessTrackedObjectId, ObjectEscapeState> _escapeStates = [];
    private readonly HashSet<ProcessTrackedObjectId> _finalizationQueuedObjects = [];

    private readonly record struct ObjectEscapeState(ProcessThreadId Instantiator, bool Escaped);

    // Reads participate in escape analysis without being classified themselves
    public void NoteAccess(ProcessThreadId threadId, ProcessTrackedObjectId? objectId)
        => UpdateEscapeState(threadId, objectId);

    public WriteKind Classify(
        ProcessThreadId threadId,
        FieldDef fieldDef,
        ProcessTrackedObjectId? objectId,
        CapturedStackTrace stack)
    {
        var processId = threadId.ProcessId;
        var declaringType = fieldDef.DeclaringType;
        var isInstantiatorExclusive = UpdateEscapeState(threadId, objectId);

        if (IsFinalizationQueued(objectId))
            return WriteKind.Finalization;

        if (objectId == null)
        {
            return IsInitializedByConstructor(processId, declaringType, stack, isStatic: true)
                ? WriteKind.Instantiation
                : WriteKind.Regular;
        }

        if (!isInstantiatorExclusive)
            return WriteKind.Regular;

        return IsInitializedByConstructor(processId, declaringType, stack, isStatic: false)
            ? WriteKind.Instantiation
            : WriteKind.Regular;
    }

    public void RecordFinalizationQueued(uint processId, ReadOnlySpan<TrackedObjectId> queuedObjectIds)
    {
        foreach (var objectId in queuedObjectIds)
            _finalizationQueuedObjects.Add(new ProcessTrackedObjectId(processId, objectId));
    }

    public bool IsFinalizationQueued(ProcessTrackedObjectId? objectId)
        => objectId is { } id && _finalizationQueuedObjects.Contains(id);

    public void RemoveTrackedObject(ProcessTrackedObjectId objectId)
    {
        _escapeStates.Remove(objectId);
        _finalizationQueuedObjects.Remove(objectId);
    }

    // Reports whether the object is still visible only to the thread that instantiated it
    private bool UpdateEscapeState(ProcessThreadId threadId, ProcessTrackedObjectId? objectId)
    {
        if (objectId is not { } objId)
            return false;

        if (!_escapeStates.TryGetValue(objId, out var state))
        {
            _escapeStates[objId] = new ObjectEscapeState(threadId, Escaped: false);
            return true;
        }

        if (state.Escaped)
            return false;

        if (state.Instantiator == threadId)
            return true;

        _escapeStates[objId] = state with { Escaped = true };
        return false;
    }

    private bool IsInitializedByConstructor(
        uint processId,
        TypeDef declaringType,
        CapturedStackTrace stack,
        bool isStatic)
    {
        return IsConstructor(stack.Top.ModuleId, stack.Top.MethodToken) ||
               stack.GetDeeperFrames().Any(frame => IsConstructor(frame.ModuleId, frame.MethodToken));

        bool IsConstructor(ModuleId moduleId, MdMethodDef methodToken) => isStatic
            ? _methodResolver.IsStaticConstructorOf(processId, moduleId, methodToken, declaringType)
            : _methodResolver.IsInstanceConstructorOf(processId, moduleId, methodToken, declaringType);
    }
}
