// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpDetect.Common.Messages;

namespace SharpDetect.Common.Instrumentation
{
    public static class MetadataGenerator
    {
        private const string managedWrapperIdentifier = "SharpDetectManagedWrapper";
        private const string helperTypeName = "EventDispatcher";
        private const string helperTypeNamespace = "SharpDetect";

        public static bool IsManagedWrapper(MethodDef methodDef)
        {
            if (methodDef.ExportInfo is MethodExportInfo info && info.Name == managedWrapperIdentifier)
                return true;

            return false;
        }

        public static TypeDef CreateHelperType(ICorLibTypes corLibTypes)
        {
            var helper = new TypeDefUser(helperTypeNamespace, helperTypeName, corLibTypes.Object.ToTypeDefOrRef())
            {
                Attributes = TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.Class | TypeAttributes.AnsiClass
            };
            return helper;
        }

        public static TypeRef CreateHelperTypeRef(ModuleDef owner)
        {
            var assemblyRef = new AssemblyRefUser(name: "System.Private.CoreLib");
            return new TypeRefUser(owner, helperTypeNamespace, helperTypeName, assemblyRef);
        }

        public static MethodDef CreateWrapper(MethodDef externMethod)
        {
            var wrapper = new MethodDefUser($".{externMethod.Name}", externMethod.MethodSig, MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | externMethod.Attributes);
            // Note this is only a dummy implementation (this is a shadow metadata entry)
            // The real implementation is supplied by profiler
            wrapper.Body = new CilBody();
            wrapper.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            wrapper.DeclaringType = externMethod.DeclaringType;
            wrapper.ExportInfo = new MethodExportInfo(managedWrapperIdentifier);
            return wrapper;
        }

        public static MethodDef CreateHelperMethod(TypeDef declaringType, MethodType methodType, ICorLibTypes corLibTypes)
        {
            var helper = new MethodDefUser(Enum.GetName(methodType), GetHelperMethodSig(methodType, corLibTypes), MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.Static | MethodAttributes.Public);
            // Note this is only a dummy implementation (this is a shadow metadata entry)
            // The real implementation is supplied by profiler
            helper.Body = new CilBody();
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            helper.DeclaringType = declaringType;
            helper.ExportInfo = new MethodExportInfo(managedWrapperIdentifier);
            return helper;
        }

        public static MethodSig GetHelperMethodSig(MethodType type, ICorLibTypes corLibTypes)
        {
            return type switch
            {
                MethodType.FieldAccess => MethodSig.CreateStatic(corLibTypes.Void, corLibTypes.Boolean, corLibTypes.UInt64),
                MethodType.FieldInstanceAccess => MethodSig.CreateStatic(corLibTypes.Void, corLibTypes.Object),
                MethodType.ArrayElementAccess => MethodSig.CreateStatic(corLibTypes.Void, corLibTypes.Boolean, corLibTypes.UInt64),
                MethodType.ArrayInstanceAccess => MethodSig.CreateStatic(corLibTypes.Void, corLibTypes.Object),
                MethodType.ArrayIndexAccess => MethodSig.CreateStatic(corLibTypes.Void, corLibTypes.Int32),
                _ => throw new NotSupportedException($"Method signature for helper method {Enum.GetName(type)} is not supported."),
            };
        }
    }
}
