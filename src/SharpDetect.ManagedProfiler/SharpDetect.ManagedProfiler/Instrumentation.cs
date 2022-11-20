using SharpDetect.Profiler.Logging;
using System.Runtime.CompilerServices;

namespace SharpDetect.Profiler
{
    internal static class Instrumentation
    {
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
            foreach (var (name, _, signature) in context.HelperMethods)
            {
                if (!module.AddMethodRef(name, eventDispatcherTypeRef, signature, out var methodRef))
                {
                    Logger.LogError($"Could not import \"{name}\" for {module.Name}");
                    return HResult.E_FAIL;
                }
                Logger.LogDebug($"Imported helper method \"{name}\" ({methodRef.Value}) for {module.Name}");
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
            context.HelperMethods.Add(new(name, methodDef, signature));
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
            context.HelperMethods.Add(new(name, methodDef, signature));
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
            context.HelperMethods.Add(new(name, methodDef, signature));
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
            context.HelperMethods.Add(new(name, methodDef, signature));
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
            context.HelperMethods.Add(new(name, methodDef, signature));
            return HResult.S_OK;
        }

        private unsafe static HResult CreateHelperMethod(
            InstrumentationContext context, 
            string name, 
            in Span<COR_SIGNATURE> signature, 
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

            // Tiny method header is indicated by mask 0x02
            // Upper 6 bits are used to indicate code size
            // There is only one 1-byte instruction that we need
            // The instruction is return (RET) with value 0x2A
            // Therefore, the code size is 1
            byte header = 0b0000_0110;
            byte code = 0x2A;

            var coreModule = context.CoreModule;
            var methodBody = coreModule.AllocMethodBody(2);
            if (methodBody == IntPtr.Zero)
            {
                // Could not allocate memory for method body
                Logger.LogError($"Could not allocate method body for method {name}");
                return HResult.E_FAIL;
            }

            // Set method body
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
    }
}
