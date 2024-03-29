﻿// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpDetect.Common;
using SharpDetect.Common.ControlFlow;
using SharpDetect.Common.Instrumentation;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Services.Descriptors;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Instrumentation.Stubs;
using SharpDetect.Instrumentation.Utilities;

namespace SharpDetect.Instrumentation.Injectors
{
    internal class FieldEventsInjector : InjectorBase
    {
        private const string couldNotFindPushFieldInstanceInstructionError = "Could not find instruction that pushes field instance on evaluation stack.";
        internal enum AccessType { Instance, Static }
        internal Dictionary<Code, Action<MethodDef, int, ulong, UnresolvedMethodStubs>> OpCodeInjectors { get; }
        internal Dictionary<Code, (AnalysisEventType EventType, AccessType AccessType)> EventTypes { get; }

        private IMethod? dummyFieldAccessRef;
        private IMethod? dummyFieldInstanceAccessRef;

        public FieldEventsInjector(IModuleBindContext moduleBindContext, IMethodDescriptorRegistry methodDescriptorRegistry)
            : base(moduleBindContext, methodDescriptorRegistry)
        {
            OpCodeInjectors = new Dictionary<Code, Action<MethodDef, int, ulong, UnresolvedMethodStubs>>()
            {
                // Load instance field
                { Code.Ldfld, new Action<MethodDef, int, ulong, UnresolvedMethodStubs>((m, i, e, s) => InjectLoadStoreInstanceField(m, i, e, false, s))},
                // Load static field
                { Code.Ldsfld, new Action<MethodDef, int, ulong, UnresolvedMethodStubs>((m, i, e, s) => InjectLoadStoreStaticField(m, i, e, false, s))},
                // Write instance field
                { Code.Stfld, new Action<MethodDef, int, ulong, UnresolvedMethodStubs>((m, i, e, s) => InjectLoadStoreInstanceField(m, i, e, true, s))},
                // Write static field
                { Code.Stsfld, new Action<MethodDef, int, ulong, UnresolvedMethodStubs>((m, i, e, s) => InjectLoadStoreStaticField(m, i, e, true, s))}
            };
            EventTypes = new Dictionary<Code, (AnalysisEventType, AccessType)>()
            {
                { Code.Ldfld, (AnalysisEventType.FieldRead, AccessType.Instance) },
                { Code.Ldsfld, (AnalysisEventType.FieldRead, AccessType.Static) },
                { Code.Stfld, (AnalysisEventType.FieldWrite, AccessType.Instance) },
                { Code.Stsfld, (AnalysisEventType.FieldWrite, AccessType.Static) }
            };
        }

        private void InjectLoadStoreStaticField(MethodDef method, int instructionIndex, ulong eventId, bool isWrite, UnresolvedMethodStubs stubs)
        {
            var instruction = method.Body.Instructions[instructionIndex];
            var pushInstruction = Instruction.Create(OpCodes.Call, CreateStub(method.Module, MethodType.FieldInstanceAccess, ref dummyFieldInstanceAccessRef));
            var callInstruction = Instruction.Create(OpCodes.Call, CreateStub(method.Module, MethodType.FieldAccess, ref dummyFieldAccessRef));
            method.InjectAfter(instruction, new Instruction[]
            {
                // Load null as instance
                Instruction.Create(OpCodes.Ldnull),
                // Call
                pushInstruction,
                // Load flag (READ/WRITE)
                Instruction.Create((isWrite) ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0),
                // Load event id
                Instruction.Create(OpCodes.Ldc_I8, (long)eventId),
                // Call
                callInstruction
            });

            // Mark stubs for fixing during assembling
            stubs.Add(pushInstruction, HelperOrWrapperReferenceStub.CreateHelperMethodReferenceStub(MethodType.FieldInstanceAccess));
            stubs.Add(callInstruction, HelperOrWrapperReferenceStub.CreateHelperMethodReferenceStub(MethodType.FieldAccess));
        }

        private void InjectLoadStoreInstanceField(MethodDef method, int instructionIndex, ulong eventId, bool isWrite, UnresolvedMethodStubs stubs)
        {
            var instruction = method.Body.Instructions[instructionIndex];
            Instruction? pushInstruction = null;
            if (isWrite && !EvaluationStackHelper.TryFindFieldWriteInstanceInfo(method, instructionIndex, out pushInstruction))
            {
                // Stfld
                throw new InvalidProgramException(couldNotFindPushFieldInstanceInstructionError);
            }
            else if (!isWrite && !EvaluationStackHelper.TryFindFieldReadInstanceInfo(method, instructionIndex, out pushInstruction))
            {
                // Ldfld
                throw new InvalidProgramException(couldNotFindPushFieldInstanceInstructionError);
            }

            // Capture instance
            var fieldInstanceAccessInstruction = Instruction.Create(OpCodes.Call, CreateStub(method.Module, MethodType.FieldInstanceAccess, ref dummyFieldInstanceAccessRef));
            method.InjectAfter(pushInstruction!, new Instruction[]
            {
                // Duplicate
                Instruction.Create(OpCodes.Dup),
                // Call
                fieldInstanceAccessInstruction
            });
            // Capture field information
            var fieldAccessInstruction = Instruction.Create(OpCodes.Call, CreateStub(method.Module, MethodType.FieldAccess, ref dummyFieldAccessRef));
            method.InjectAfter(instruction, new Instruction[]
            {
                // Load flag (READ/WRITE)
                Instruction.Create((isWrite) ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0),
                // Load event id
                Instruction.Create(OpCodes.Ldc_I8, (long)eventId),
                // Call
                fieldAccessInstruction
            });

            stubs.Add(fieldAccessInstruction, HelperOrWrapperReferenceStub.CreateHelperMethodReferenceStub(MethodType.FieldAccess));
            stubs.Add(fieldInstanceAccessInstruction, HelperOrWrapperReferenceStub.CreateHelperMethodReferenceStub(MethodType.FieldInstanceAccess));
        }

        public override AnalysisEventType? CanInject(MethodDef methodDef, Instruction instruction)
        {
            var opcode = instruction.OpCode.Code;
            if (!EventTypes.TryGetValue(opcode, out var item))
                return null;

            // Static field
            if (item.AccessType == AccessType.Static)
                return item.EventType;

            if (((IField)instruction.Operand).DeclaringType.IsValueType)
            {
                // TODO: support fields on value types
                // We must identify:
                //    1) ShadowObject that contains this field
                //    2) Offset within the object

                return null;
            }

            return item.EventType;
        }

        public override void Inject(MethodDef methodDef, int instructionIndex, ulong eventId, UnresolvedMethodStubs stubs)
        {
            var instruction = methodDef.Body.Instructions[instructionIndex];
            var injector = OpCodeInjectors[instruction.OpCode.Code];
            injector.Invoke(methodDef, instructionIndex, eventId, stubs);
        }
    }
}
