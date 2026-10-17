// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using dnlib.DotNet;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.Plugins.DataRace.Common;

namespace SharpDetect.Plugins.DataRace.FastTrack;

// The runtime completes a type initializer before any thread may access the type's static fields (ECMA-335 II.10.5.3)
internal sealed class TypeInitializationClocks(
    ThreadClockTable threadClocks,
    FieldResolver fieldResolver,
    MethodResolver methodResolver)
{
    private sealed class Slot
    {
        public VectorClock? Clock;
        public HashSet<ProcessThreadId>? Waiters;
        public int RunningInitializers;
    }

    private readonly Dictionary<uint, Dictionary<TypeDef, Slot>> _slotsByProcess = [];
    private readonly Dictionary<FieldDefOrRef, Slot?> _slotsByField = [];
    private readonly Dictionary<ProcessThreadId, List<Slot>> _runningInitializers = [];

    public void Enter(ProcessThreadId threadId, ModuleId moduleId, MdMethodDef typeInitializerToken)
    {
        if (!TryGetInitializedSlot(threadId, moduleId, typeInitializerToken, out var slot))
            return;

        if (!_runningInitializers.TryGetValue(threadId, out var running))
        {
            running = [];
            _runningInitializers.Add(threadId, running);
        }

        running.Add(slot);
        slot.RunningInitializers++;
    }

    public void Fail(ProcessThreadId threadId, ModuleId moduleId, MdMethodDef typeInitializerToken)
    {
        if (!TryGetInitializedSlot(threadId, moduleId, typeInitializerToken, out var slot))
            return;

        RemoveRunning(threadId, slot);
        ForgetWaitersOnceIdle(slot);
    }

    public void Release(ProcessThreadId threadId, ModuleId moduleId, MdMethodDef typeInitializerToken)
    {
        if (!TryGetInitializedSlot(threadId, moduleId, typeInitializerToken, out var slot))
            return;

        RemoveRunning(threadId, slot);
        var threadVc = threadClocks.GetOrCreate(threadId);
        if (slot.Clock is { } slotVc)
            slotVc.Join(threadVc);
        else
            slot.Clock = threadVc.Clone();

        if (slot.Waiters is { } waiters)
        {
            foreach (var waiter in waiters)
                threadClocks.GetOrCreate(waiter).Join(slot.Clock);
        }

        ForgetWaitersOnceIdle(slot);
        threadVc.Increment(threadId);
    }

    public void Acquire(ProcessThreadId threadId, ModuleId moduleId, MdToken fieldToken)
    {
        var fieldReference = new FieldDefOrRef(threadId.ProcessId, moduleId, fieldToken);
        if (!_slotsByField.TryGetValue(fieldReference, out var slot))
        {
            slot = fieldResolver.TryResolve(threadId.ProcessId, moduleId, fieldToken, out var fieldDef, out _)
                ? GetSlot(threadId.ProcessId, fieldDef!.DeclaringType)
                : null;
            _slotsByField.Add(fieldReference, slot);
        }

        if (slot is null)
            return;

        if (slot.Clock is { } slotVc)
            threadClocks.GetOrCreate(threadId).Join(slotVc);
        
        if (slot.Clock is null || slot.RunningInitializers > 0)
            (slot.Waiters ??= []).Add(threadId);
    }
    
    public bool IsRunningInitializerOf(ProcessThreadId threadId, TypeDef type)
    {
        return _runningInitializers.TryGetValue(threadId, out var running) &&
               running.Contains(GetSlot(threadId.ProcessId, type));
    }

    private bool TryGetInitializedSlot(
        ProcessThreadId threadId,
        ModuleId moduleId,
        MdMethodDef typeInitializerToken,
        [NotNullWhen(true)] out Slot? slot)
    {
        if (!methodResolver.TryGetTypeInitializedBy(threadId.ProcessId, moduleId, typeInitializerToken, out var initializedType))
        {
            slot = null;
            return false;
        }

        slot = GetSlot(threadId.ProcessId, initializedType!);
        return true;
    }

    private void RemoveRunning(ProcessThreadId threadId, Slot slot)
    {
        if (!_runningInitializers.TryGetValue(threadId, out var running))
            return;

        var index = running.LastIndexOf(slot);
        if (index >= 0)
        {
            running.RemoveAt(index);
            slot.RunningInitializers--;
        }

        if (running.Count == 0)
            _runningInitializers.Remove(threadId);
    }

    private static void ForgetWaitersOnceIdle(Slot slot)
    {
        if (slot.RunningInitializers == 0)
            slot.Waiters = null;
    }

    private Slot GetSlot(uint processId, TypeDef type)
    {
        if (!_slotsByProcess.TryGetValue(processId, out var slots))
        {
            slots = new Dictionary<TypeDef, Slot>(TypeEqualityComparer.Instance);
            _slotsByProcess.Add(processId, slots);
        }

        if (!slots.TryGetValue(type, out var slot))
        {
            slot = new Slot();
            slots.Add(type, slot);
        }

        return slot;
    }
}
