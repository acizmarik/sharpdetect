using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpDetect.Common;
using SharpDetect.Common.ControlFlow;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Services.Descriptors;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Instrumentation.Stubs;
using SharpDetect.Instrumentation.Utilities;

namespace SharpDetect.Instrumentation.Injectors
{
    internal class ArrayEventsInjector : InjectorBase
    {
        private const string couldNotFindPushArrayInstanceInstructionError = "Could not find instruction that pushes array instance on evaluation stack.";
        private const string couldNotFindPushArrayIndexInstructionError = "Could not find instruction that pushes array index on evaluation stack.";
        internal Dictionary<Code, (AnalysisEventType Type, bool IsWrite)> EventTypes { get; }
        private Action<MethodDef, int, ulong, bool, UnresolvedMethodStubs> injector;
        private IMethod? dummyArrayElementAccessRef;
        private IMethod? dummyArrayInstanceAccessRef;
        private IMethod? dummyArrayIndexAccessRef;

        public ArrayEventsInjector(IModuleBindContext moduleBindContext, IMethodDescriptorRegistry methodDescriptorRegistry)
            : base(moduleBindContext, methodDescriptorRegistry)
        {
            injector = (m, i, e, f, s) => InjectLoadStoreArrayElement(m, i, e, f, s);
            EventTypes = new Dictionary<Code, (AnalysisEventType Type, bool IsWrite)>()
            {
                { Code.Ldelem, (AnalysisEventType.ArrayElementRead, false) },
                { Code.Ldelem_I, (AnalysisEventType.ArrayElementRead, false) },
                { Code.Ldelem_I1, (AnalysisEventType.ArrayElementRead, false) },
                { Code.Ldelem_I2, (AnalysisEventType.ArrayElementRead, false) },
                { Code.Ldelem_I4, (AnalysisEventType.ArrayElementRead, false) },
                { Code.Ldelem_I8, (AnalysisEventType.ArrayElementRead, false) },
                { Code.Ldelem_R4, (AnalysisEventType.ArrayElementRead, false) },
                { Code.Ldelem_R8, (AnalysisEventType.ArrayElementRead, false) },
                { Code.Ldelem_Ref, (AnalysisEventType.ArrayElementRead, false) },
                { Code.Ldelem_U1, (AnalysisEventType.ArrayElementRead, false) },
                { Code.Ldelem_U2, (AnalysisEventType.ArrayElementRead, false) },
                { Code.Ldelem_U4, (AnalysisEventType.ArrayElementRead, false) },
                { Code.Stelem, (AnalysisEventType.ArrayElementWrite, true) },
                { Code.Stelem_I, (AnalysisEventType.ArrayElementWrite, true) },
                { Code.Stelem_I1, (AnalysisEventType.ArrayElementWrite, true) },
                { Code.Stelem_I2, (AnalysisEventType.ArrayElementWrite, true) },
                { Code.Stelem_I4, (AnalysisEventType.ArrayElementWrite, true) },
                { Code.Stelem_I8, (AnalysisEventType.ArrayElementWrite, true) },
                { Code.Stelem_R4, (AnalysisEventType.ArrayElementWrite, true) },
                { Code.Stelem_R8, (AnalysisEventType.ArrayElementWrite, true) },
                { Code.Stelem_Ref, (AnalysisEventType.ArrayElementWrite, true) }
            };
        }

        private void InjectLoadStoreArrayElement(MethodDef method, int instructionIndex, ulong eventId, bool isWrite, UnresolvedMethodStubs stubs)
        {
            var instruction = method.Body.Instructions[instructionIndex];
            var arrayElementAccessInstruction = Instruction.Create(OpCodes.Call, CreateStub(MethodType.ArrayElementAccess, ref dummyArrayElementAccessRef));
            var arrayInstanceAccessInstruction = Instruction.Create(OpCodes.Call, CreateStub(MethodType.ArrayInstanceAccess, ref dummyArrayInstanceAccessRef));
            var arrayIndexAccessInstruction = Instruction.Create(OpCodes.Call, CreateStub(MethodType.ArrayIndexAccess, ref dummyArrayIndexAccessRef));
            
            // Retrieve instruction where the array instance gets pushed onto evaluation stack
            Instruction? pushArrayInstanceInstruction = null;
            if (isWrite && !EvaluationStackHelper.TryFindArrayInstanceWriteElementInfo(method, instructionIndex, out pushArrayInstanceInstruction))
                throw new InvalidProgramException(couldNotFindPushArrayInstanceInstructionError);
            if (!isWrite && !EvaluationStackHelper.TryFindArrayInstanceReadElementInfo(method, instructionIndex, out pushArrayInstanceInstruction))
                throw new InvalidProgramException(couldNotFindPushArrayInstanceInstructionError);

            // Retrieve instruction where the array index gets pushed onto evaluation stack
            Instruction? pushArrayIndexInstruction = null;
            if (isWrite && !EvaluationStackHelper.TryFindArrayIndexWriteInfo(method, instructionIndex, out pushArrayIndexInstruction))
                throw new InvalidProgramException(couldNotFindPushArrayIndexInstructionError);
            if (!isWrite && !EvaluationStackHelper.TryFindArrayIndexReadInfo(method, instructionIndex, out pushArrayIndexInstruction))
                throw new InvalidProgramException(couldNotFindPushArrayIndexInstructionError);

            // Pass information about array instance
            method.InjectAfter(pushArrayInstanceInstruction!, new Instruction[]
            {
                // Duplicate reference to the array instance
                Instruction.Create(OpCodes.Dup),
                // Call
                arrayInstanceAccessInstruction
            });

            // Pass information about array index
            method.InjectAfter(pushArrayIndexInstruction!, new Instruction[]
            {
                // Duplicate index to the array element
                Instruction.Create(OpCodes.Dup),
                // Call
                arrayIndexAccessInstruction
            });

            // Pass information about array element access
            method.InjectAfter(instruction, new Instruction[]
            {
                // Load flag (READ/WRITE)
                Instruction.Create((isWrite) ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0),
                // Load event id
                Instruction.Create(OpCodes.Ldc_I8, (long)eventId),
                // Call
                arrayElementAccessInstruction
            });

            stubs.Add(arrayElementAccessInstruction, HelperOrWrapperReferenceStub.CreateHelperMethodReferenceStub(MethodType.ArrayElementAccess));
            stubs.Add(arrayInstanceAccessInstruction, HelperOrWrapperReferenceStub.CreateHelperMethodReferenceStub(MethodType.ArrayInstanceAccess));
            stubs.Add(arrayIndexAccessInstruction, HelperOrWrapperReferenceStub.CreateHelperMethodReferenceStub(MethodType.ArrayIndexAccess));
        }

        private IMethod CreateStub(MethodType type, ref IMethod? methodRef)
        {
            if (methodRef is null)
            {
                var identifier = MethodDescriptorRegistry.GetCoreLibraryDescriptor().Methods
                    .SingleOrDefault(record => record.Identifier.IsInjected && record.Identifier.Name == Enum.GetName(typeof(MethodType), type)).Identifier;
                var coreLibModule = ModuleBindContext.GetCoreLibModule(ProcessId);
                var coreLibTypes = coreLibModule.CorLibTypes;
                methodRef = new MemberRefUser(coreLibModule, identifier.Name, MetadataGenerator.GetHelperMethodSig(type, coreLibTypes));
            }

            return methodRef;
        }

        public override AnalysisEventType? CanInject(MethodDef methodDef, Instruction instruction)
        {
            var opcode = instruction.OpCode.Code;
            if (!EventTypes.TryGetValue(opcode, out var item))
                return null;

            return item.Type;
        }

        public override void Inject(MethodDef methodDef, int instructionIndex, ulong eventId, UnresolvedMethodStubs stubs)
        {
            var instruction = methodDef.Body.Instructions[instructionIndex];
            var (type, isWrite) = EventTypes[instruction.OpCode.Code];
            injector.Invoke(methodDef, instructionIndex, eventId, isWrite, stubs);
        }
    }
}
