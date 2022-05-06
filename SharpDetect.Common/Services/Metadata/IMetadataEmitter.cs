using dnlib.DotNet;
using SharpDetect.Common.Messages;

namespace SharpDetect.Common.Services.Metadata
{
    public interface IMetadataEmitter
    {
        int ProcessId { get; }

        void Emit(ModuleInfo module, TypeDef type, MDToken token);
        void Emit(ModuleInfo module, MethodDef method, MDToken token);

        void Bind(TypeDef type, TypeInfo reference);
        void Bind(MethodDef method, FunctionInfo reference);
        void Bind(MethodType type, FunctionInfo reference);
    }
}
