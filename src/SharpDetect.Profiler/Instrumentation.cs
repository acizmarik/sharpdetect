// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common.Messages;
using SharpDetect.Profiler.Logging;
using System.Runtime.CompilerServices;

namespace SharpDetect.Profiler;

internal static class Instrumentation
{
    public static event Action<(Module Module, MdTypeDef TypeDef)>? TypeInjected;
    public static event Action<(Module Module, MdTypeDef TypeDef, MdMethodDef MethodDef, MethodType Type)>? HelperMethodInjected;
    public static event Action<(Module Module, MdTypeRef TypeRef, MdMemberRef MethodRef, MethodType Type)>? HelperMethodImported;
    public static event Action<(Module Module, MdTypeDef TypeDef, MdMethodDef OriginalMethodDef, MdMethodDef WrapperMethodDef)>? WrapperMethodInjected;
    public static event Action<(Module DefModule, MdTypeDef DefTypeDef, MdMethodDef DefMethodDef, Module RefModule, MdTypeRef DefTypeRef, MdMemberRef RefMethodRef)>? WrapperMethodImported;

    public const string DispatcherTypeName = "SharpDetect.EventDispatcher";
    public const string ObjectTypeName = "System.Object";

    public static HResult InjectEventDispatcherType(ref InstrumentationContext context)
    {
        // This is a reference type => set base class to object
        var coreModule = context.CoreModule;
        if (!coreModule.FindTypeDef(ObjectTypeName, out var objectTypeDef))
            return HResult.E_FAIL;

        // Flags for a regular static class
        const CorTypeAttr flags =
            CorTypeAttr.tdPublic |
            CorTypeAttr.tdAutoClass |
            CorTypeAttr.tdAnsiClass |
            CorTypeAttr.tdAbstract |
            CorTypeAttr.tdSealed |
            CorTypeAttr.tdBeforeFieldInit;

        var mdToken = Unsafe.As<MdTypeDef, MdToken>(ref objectTypeDef);
        if (!coreModule.AddTypeDef(DispatcherTypeName, flags, mdToken, out var typeDef))
            return HResult.E_FAIL;

        context = context with { EventDispatcherTypeDef = typeDef };
        Logger.LogDebug($"Emitted helper type \"{DispatcherTypeName}\" ({context.EventDispatcherTypeDef.Value}) into {coreModule.Name}");
        TypeInjected?.Invoke((coreModule, typeDef));
        return HResult.S_OK;
    }

    public static HResult InjectWrapperMethod(
        InstrumentationContext context, 
        Module module, 
        MdTypeDef typeDef, 
        MdMethodDef externMethodDef, 
        ushort parametersCount,
        out MdMethodDef wrapperMethodDef)
    {
        // Generate wrapper method for an extern method (no IL implementation)
        if (!CreateWrapperMethod(context, module, typeDef, externMethodDef, parametersCount, out wrapperMethodDef, out var name, out var wrapperSignature))
            return HResult.E_FAIL;

        // Obtain type name of the declaring type
        if (!module.GetTypeProps(typeDef, out var typeName))
            return HResult.E_FAIL;

        var entry = new InstrumentationContext.WrappedMethodInfo(
            name![1..],
            module,
            typeDef,
            wrapperMethodDef,
            wrapperSignature.ToArray(),
        parametersCount);

        Logger.LogDebug($"Emitted wrapper method \"{name}\" into {context.CoreModule.Name}");
        context.AddWrapperMethod(module, typeName!, entry);
        WrapperMethodInjected?.Invoke((module, typeDef, externMethodDef, wrapperMethodDef));
        return HResult.S_OK;
    }

    public static HResult ImportWrapperMethods(
        InstrumentationContext context,
        Assembly assembly,
        Module module)
    {
        // Check all injected method wrappers
        foreach (var (fromModule, wrappers) in context.WrappedMethods)
        {
            // Assembly is not referenced => skip
            var assemblyRef = assembly.References.FirstOrDefault(r => 
                fromModule.FullPath.Contains($"{r.Name}.dll", StringComparison.OrdinalIgnoreCase))?.AssemblyRef;
            if (assemblyRef is null)
                continue;

            foreach (var (fromType, methods) in wrappers)
            {
                if (!module.FindTypeRef(assemblyRef.Value, fromType, out var typeRef) &&
                    !module.AddTypeRef(assemblyRef.Value, fromType, out typeRef))
                {
                    Logger.LogError($"Could not get or create a reference to type \"{fromType}\" for {module.Name}");
                    return HResult.E_FAIL;
                }

                foreach (var method in methods)
                {
                    var wrapperName = $".{method.Name}";
                    if (!module.FindMethodRef(wrapperName, method.Signature, typeRef, out var methodRef))
                    {
                        // Import wrapper method
                        if (!module.AddMethodRef(wrapperName, typeRef, method.Signature, out methodRef))
                        {
                            Logger.LogError($"Could not import wrapper method \"{wrapperName}\" for {module.Name}");
                            return HResult.E_FAIL;
                        }

                        Logger.LogDebug($"Imported wrapper method \"{wrapperName}\" ({methodRef.Value}) for {module.Name}");
                        WrapperMethodImported?.Invoke((method.Module, method.TypeDef, method.MethodDef, module, typeRef, methodRef));
                    }
                }
            }
        }

        return HResult.S_OK;
    }

    public static HResult InjectHelperMethods(InstrumentationContext context)
    {
        if (!InjectFieldAccessHelperMethod(context))
            return HResult.E_FAIL;

        if (!InjectFieldInstanceAccessHelperMethod(context))
            return HResult.E_FAIL;

        if (!InjectArrayElementAccessHelperMethod(context))
            return HResult.E_FAIL;

        if (!InjectArrayInstanceAccessHelperMethod(context))
            return HResult.E_FAIL;

        if (!InjectArrayIndexAccessHelperMethod(context))
            return HResult.E_FAIL;

        return HResult.S_OK;
    }

    public static HResult ImportHelperMethods(
        InstrumentationContext context,
        Module module,
        MdTypeRef eventDispatcherTypeRef)
    {
        foreach (var (name, type, _, signature) in context.HelperMethods)
        {
            if (!module.AddMethodRef(name, eventDispatcherTypeRef, signature, out var methodRef))
            {
                Logger.LogError($"Could not import \"{name}\" for {module.Name}");
                return HResult.E_FAIL;
            }
            Logger.LogDebug($"Imported helper method \"{name}\" ({methodRef.Value}) for {module.Name}");
            HelperMethodImported?.Invoke((module, eventDispatcherTypeRef, methodRef, type));
        }

        return HResult.S_OK;
    }

    private static HResult InjectFieldAccessHelperMethod(InstrumentationContext context)
    {
        const string name = "FieldAccess";
        var signature = new COR_SIGNATURE[]
        {
            (byte)CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
            2,
            (byte)CorElementType.ELEMENT_TYPE_VOID,
            (byte)CorElementType.ELEMENT_TYPE_BOOLEAN,
            (byte)CorElementType.ELEMENT_TYPE_I8
        };
        if (!CreateHelperMethod(context, name, signature, out var methodDef))
            return HResult.E_FAIL;

        Logger.LogDebug($"Emitted helper method \"{name}\" ({methodDef.Value}) into {context.CoreModule.Name}");
        context.AddHelperMethod(new(name, MethodType.FieldAccess, methodDef, signature));
        HelperMethodInjected?.Invoke((context.CoreModule, context.EventDispatcherTypeDef, methodDef, MethodType.FieldAccess));
        return HResult.S_OK;
    }

    private static HResult InjectFieldInstanceAccessHelperMethod(InstrumentationContext context)
    {
        const string name = "FieldInstanceRefAccess";
        var signature = new COR_SIGNATURE[]
        {
            (byte)CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
            1,
            (byte)CorElementType.ELEMENT_TYPE_VOID,
            (byte)CorElementType.ELEMENT_TYPE_OBJECT,
        };
        if (!CreateHelperMethod(context, name, signature, out var methodDef))
            return HResult.E_FAIL;

        Logger.LogDebug($"Emitted helper method \"{name}\" ({methodDef.Value}) into {context.CoreModule.Name}");
        context.AddHelperMethod(new(name, MethodType.FieldInstanceAccess, methodDef, signature));
        HelperMethodInjected?.Invoke((context.CoreModule, context.EventDispatcherTypeDef, methodDef, MethodType.FieldInstanceAccess));
        return HResult.S_OK;
    }

    private static HResult InjectArrayElementAccessHelperMethod(InstrumentationContext context)
    {
        const string name = "ArrayElementAccess";
        var signature = new COR_SIGNATURE[]
        {
            (byte)CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
            2,
            (byte)CorElementType.ELEMENT_TYPE_VOID,
            (byte)CorElementType.ELEMENT_TYPE_BOOLEAN,
            (byte)CorElementType.ELEMENT_TYPE_I8
        };
        if (!CreateHelperMethod(context, name, signature, out var methodDef))
            return HResult.E_FAIL;

        Logger.LogDebug($"Emitted helper method \"{name}\" ({methodDef.Value}) into {context.CoreModule.Name}");
        context.AddHelperMethod(new(name, MethodType.ArrayElementAccess, methodDef, signature));
        HelperMethodInjected?.Invoke((context.CoreModule, context.EventDispatcherTypeDef, methodDef, MethodType.ArrayElementAccess));
        return HResult.S_OK;
    }

    private static HResult InjectArrayInstanceAccessHelperMethod(InstrumentationContext context)
    {
        const string name = "ArrayInstanceRefAccess";
        var signature = new COR_SIGNATURE[]
        {
            (byte)CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
            1,
            (byte)CorElementType.ELEMENT_TYPE_VOID,
            (byte)CorElementType.ELEMENT_TYPE_OBJECT,
        };
        if (!CreateHelperMethod(context, name, signature, out var methodDef))
            return HResult.E_FAIL;

        Logger.LogDebug($"Emitted helper method \"{name}\" ({methodDef.Value}) into {context.CoreModule.Name}");
        context.AddHelperMethod(new(name, MethodType.ArrayInstanceAccess, methodDef, signature));
        HelperMethodInjected?.Invoke((context.CoreModule, context.EventDispatcherTypeDef, methodDef, MethodType.ArrayInstanceAccess));
        return HResult.S_OK;
    }

    private static HResult InjectArrayIndexAccessHelperMethod(InstrumentationContext context)
    {
        const string name = "ArrayIndexAccess";
        var signature = new COR_SIGNATURE[]
        {
            (byte)CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
            1,
            (byte)CorElementType.ELEMENT_TYPE_VOID,
            (byte)CorElementType.ELEMENT_TYPE_I4,
        };
        if (!CreateHelperMethod(context, name, signature, out var methodDef))
            return HResult.E_FAIL;

        Logger.LogDebug($"Emitted helper method \"{name}\" ({methodDef.Value}) into {context.CoreModule.Name}");
        context.AddHelperMethod(new(name, MethodType.ArrayIndexAccess, methodDef, signature));
        HelperMethodInjected?.Invoke((context.CoreModule, context.EventDispatcherTypeDef, methodDef, MethodType.ArrayIndexAccess));
        return HResult.S_OK;
    }

    private unsafe static HResult CreateWrapperMethod(
        InstrumentationContext context,
        Module module,
        MdTypeDef typeDef,
        MdMethodDef externMethodDef,
        int parametersCount,
        out MdMethodDef methodDef,
        out string? name,
        out ReadOnlySpan<COR_SIGNATURE> signature)
    {
        methodDef = MdMethodDef.Nil;
        signature = default;

        // Read information about the extern method that is being wrapped
        if (!module.GetMethodProps(externMethodDef, out _, out name, out var flags, out signature))
        {
            Logger.LogError("Could not retrieve method props about an extern method for wrapping");
            return HResult.S_OK;
        }

        // Flags for an injected method (special name)
        CorMethodAttr methodFlags = flags!.Value |
            CorMethodAttr.mdSpecialName |
            CorMethodAttr.mdRTSpecialName;

        // Flags for a regular managed method
        const CorMethodImpl implFlags =
            CorMethodImpl.miIL |
            CorMethodImpl.miManaged |
            CorMethodImpl.miNoInlining |
            CorMethodImpl.miNoOptimization;

        // Wrapper name will be prefixed with a dot
        name = '.' + name;

        // Tiny method header is indicated by mask 0x02
        // Upper 6 bits will be used to indicate code size
        byte header = 0b0000_0010;
        List<byte> code = new(parametersCount + 5 /* method call */ + 1 /* return */);

        // Add instructions
        for (var i = 0; i < parametersCount; i++)
        {
            // Add Ldarg.i for each parameter
            if (i <= 3)
            {
                // Ldarg.0 - 0x02, Ldarg.1 - 0x03...
                code.Add((byte)(0x02 + i));
            }
            else
            {
                // Ldarg.s
                code.Add(0x0E);
                // uint8 arg
                code.Add((byte)i);
            }
        }
        // Call - 0x28
        code.Add(0x28);
        // uint32 arg (md token)
        code.Add((byte)externMethodDef.Value);
        code.Add((byte)(externMethodDef.Value >> 8));
        code.Add((byte)(externMethodDef.Value >> 16));
        code.Add((byte)(externMethodDef.Value >> 24));
        // Return - 0x2A
        code.Add(0x2A);

        // Set code size
        header |= (byte)(code.Count << 2);
        if (code.Count > 63)
        {
            Logger.LogError("Could not generate wrapper - the requested method body is bigger than 63 bytes");
            return HResult.E_FAIL;
        }

        // Set method body
        if (!AllocateMethodBody(module, name, size: (ulong)(code.Count + 1), out var methodBody))
            return HResult.E_FAIL;
        Unsafe.Write(methodBody.ToPointer(), header);
        for (var i = 0; i < code.Count; i++)
            Unsafe.Write((methodBody + 1 + i).ToPointer(), code[i]);

        // Create helper method definition
        if (!module.AddMethodDef(name, methodFlags, typeDef, in signature, implFlags, out methodDef))
            return HResult.E_FAIL;
        // Set method body
        if (!context.CorProfilerInfo.SetILFunctionBody(module.Id, methodDef, methodBody))
            return HResult.E_FAIL;

        return HResult.S_OK;
    }

    private unsafe static HResult CreateHelperMethod(
        InstrumentationContext context, 
        string name, 
        in ReadOnlySpan<COR_SIGNATURE> signature, 
        out MdMethodDef methodDef)
    {
        methodDef = MdMethodDef.Nil;

        // Flags for a regular static method
        const CorMethodAttr flags =
            CorMethodAttr.mdPublic |
            CorMethodAttr.mdStatic;

        // Flags for a regular managed method
        const CorMethodImpl implFlags =
            CorMethodImpl.miIL |
            CorMethodImpl.miManaged;

        // Tiny method header
        // There is only one 1-byte instruction that we need
        // The instruction is return (RET) with value 0x2A
        // Therefore, the code size is 1
        byte header = 0b0000_0110;
        byte code = 0x2A;

        var coreModule = context.CoreModule;

        // Set method body
        if (!AllocateMethodBody(coreModule, name, size: 2, out var methodBody))
            return HResult.S_OK;
        Unsafe.Write(methodBody.ToPointer(), header);
        Unsafe.Write((methodBody + 1).ToPointer(), code);

        // Create helper method definition
        if (!coreModule.AddMethodDef(name, flags, context.EventDispatcherTypeDef, in signature, implFlags, out methodDef))
            return HResult.E_FAIL;
        // Set method body
        if (!context.CorProfilerInfo.SetILFunctionBody(coreModule.Id, methodDef, methodBody))
            return HResult.E_FAIL;

        return HResult.S_OK;
    }

    private static HResult AllocateMethodBody(Module module, string name, ulong size, out IntPtr methodBody)
    {
        methodBody = module.AllocMethodBody(size);
        if (methodBody == IntPtr.Zero)
        {
            // Could not allocate memory for method body
            Logger.LogError($"Could not allocate method body for method {name}");
            return HResult.E_FAIL;
        }

        return HResult.S_OK;
    }
}
