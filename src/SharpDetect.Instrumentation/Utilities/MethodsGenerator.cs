using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpDetect.Common.Messages;

namespace SharpDetect.Instrumentation.Utilities
{
    public static class MethodsGenerator
    {
        private const string managedWrapperIdentifier = "SharpDetectManagedWrapper";

        public static bool IsManagedWrapper(MethodDef methodDef)
        {
            if (methodDef.ExportInfo is MethodExportInfo info && info.Name == managedWrapperIdentifier)
                return true;

            return false;
        }

        public static MethodDef CreateWrapper(MethodDef externMethod)
        {
            var wrapper = new MethodDefUser($".{externMethod.Name}", externMethod.MethodSig, MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | externMethod.Attributes);
            wrapper.Body = new CilBody();
            wrapper.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            wrapper.DeclaringType = externMethod.DeclaringType;
            wrapper.ExportInfo = new MethodExportInfo(managedWrapperIdentifier);
            return wrapper;
        }

        public static MethodDef CreateHelper(MethodType type, ICorLibTypes corLibTypes)
        {
            var helper = new MethodDefUser(Enum.GetName(type), GetHelperMethodSig(type, corLibTypes), MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.Static | MethodAttributes.Public);
            // Note: there is no need to implement this here, the implementation is straight-forward and supplied by profiler
            return helper;
        }

        public static MethodSig GetHelperMethodSig(MethodType type, ICorLibTypes corLibTypes)
        {
            return type switch
            {
                MethodType.FieldAccess => MethodSig.CreateStatic(corLibTypes.Void, corLibTypes.Boolean, corLibTypes.Object),
                MethodType.FieldInstanceAccess => MethodSig.CreateStatic(corLibTypes.Void, corLibTypes.Object),
                MethodType.ArrayElementAccess => MethodSig.CreateStatic(corLibTypes.Void, corLibTypes.Boolean, corLibTypes.Object),
                MethodType.ArrayInstanceAccess => MethodSig.CreateStatic(corLibTypes.Void, corLibTypes.Object),
                MethodType.ArrayIndexAccess => MethodSig.CreateStatic(corLibTypes.Void, corLibTypes.Int32),
                _ => throw new NotSupportedException($"Method signature for helper method {Enum.GetName(type)} is not supported."),
            };
        }
    }
}
