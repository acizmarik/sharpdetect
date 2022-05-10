using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpDetect.Common;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Services.Metadata;

namespace SharpDetect.Metadata
{
    internal class MetadataEmitter : IMetadataEmitter
    {
        public int ProcessId { get; }
        private readonly InjectedData state;

        public MetadataEmitter(int processId, InjectedData state)
        {
            ProcessId = processId;
            this.state = state;
        }

        public void Emit(ModuleInfo owner, TypeDef definition, MDToken token)
        {
            state.AddTypeDef(owner, definition, token);
        }

        public void Emit(ModuleInfo owner, MethodDef definition, MDToken token)
        {
            state.AddMethodDef(owner, definition, token);
        }

        public void Bind(TypeDef defintion, TypeInfo reference)
        {
            // Note: there is currently a single type being always injected
            // i.e. we do not need to distinguish between them
            state.AddEventDispatcherReference(new(reference.ModuleId), reference.TypeToken);
        }

        public void Bind(MethodDef method, FunctionInfo reference)
        {
            state.AddMethodReference(new(reference.ModuleId), method, reference.FunctionToken);
        }

        public void Bind(MethodType type, FunctionInfo reference)
        {
            state.AddMethodReference(new(reference.ModuleId), type, reference.FunctionToken);
        }
    }
}
