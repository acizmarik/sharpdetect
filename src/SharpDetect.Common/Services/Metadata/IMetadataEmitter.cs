using dnlib.DotNet;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Metadata;

namespace SharpDetect.Common.Services.Metadata
{
    public interface IMetadataEmitter
    {
        int ProcessId { get; }

        /// <summary>
        /// Emits a new type into the specified module
        /// </summary>
        /// <param name="module">Module to emit to</param>
        /// <param name="type">Type to emit</param>
        /// <param name="token">Actual metadata token used by target process</param>
        void Emit(ModuleInfo module, TypeDef type, MDToken token);

        /// <summary>
        /// Emits a new method into the specified module
        /// </summary>
        /// <param name="module">Module to emit to</param>
        /// <param name="method">Method to emit</param>
        /// <param name="token">Actual metadata token used by target process</param>
        void Emit(ModuleInfo module, MethodDef method, MDToken token);

        /// <summary>
        /// Binds a TypeDef (creates a reference) from a different module
        /// </summary>
        /// <param name="type">Type definition to bind from a different module</param>
        /// <param name="reference">Actual type reference used by target process</param>
        void Bind(TypeDef type, TypeInfo reference);

        /// <summary>
        /// Binds a MethodDef (creates a reference) from a different module. 
        /// Extern method is wrapped by a managed wrapper. Managed wrapper is then referenced by a different module
        /// </summary>
        /// <param name="externMethod">Wrapped extern method definition</param>
        /// <param name="wrapperMethod">Managed wrapper definition for the given extern method</param>
        /// <param name="wrapperReference">Actual method reference used by target process</param>
        void Bind(ExternMethodDef externMethod, WrapperMethodDef wrapperMethod, FunctionInfo wrapperReference);

        /// <summary>
        /// Binds a MethodDef (creates a reference) from a different module. 
        /// Helper method is a managed stub that represents a specific action and is uniquely identified by the MethodType
        /// </summary>
        /// <param name="type">Helper method type</param>
        /// <param name="reference">Actual method reference used by target process</param>
        void Bind(MethodType type, FunctionInfo reference);
    }
}
