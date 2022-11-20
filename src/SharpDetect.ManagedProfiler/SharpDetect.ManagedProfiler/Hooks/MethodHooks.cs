using SharpDetect.Profiler.Logging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpDetect.Profiler.Hooks
{
    internal static unsafe class MethodHooks
    {
        public static HResult Register(ICorProfilerInfo3 corProfilerInfo, AsmUtilities asmUtilities)
        {
            // Set function to user-type mapper
            var funcPtr = (delegate* unmanaged[Stdcall]<FunctionId, nint, bool*, nint>)&MethodHooks.FunctionIdMapper2;
            if (!corProfilerInfo.SetFunctionIDMapper2(funcPtr, IntPtr.Zero))
            {
                Logger.LogError($"Could not set {nameof(ICorProfilerInfo3.SetFunctionIDMapper2)}");
                return HResult.E_FAIL;
            }

            // Set entry exit hooks
            var methodEnterHookPtr = (IntPtr)(delegate* unmanaged[Stdcall]<nint, COR_PRF_ELT_INFO, void>)&MethodHooks.MethodEnterHook;
            var methodLeaveHookPtr = (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, COR_PRF_ELT_INFO, void>)&MethodHooks.MethodLeaveHook;
            var methodTailcallHookPtr = (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, COR_PRF_ELT_INFO, void>)&MethodHooks.MethodTailcallHook;
            var methodEnterStub = asmUtilities.GenerateStub(methodEnterHookPtr);
            var methodLeaveStub = asmUtilities.GenerateStub(methodLeaveHookPtr);
            var methodTailcallStub = asmUtilities.GenerateStub(methodTailcallHookPtr);

            if (!corProfilerInfo.SetEnterLeaveFunctionHooks3WithInfo(
                methodEnterStub,
                methodLeaveStub,
                methodTailcallStub))
            {
                Logger.LogError($"Could not set method enter/leave hooks");
                return HResult.E_FAIL;
            }

            return HResult.S_OK;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        public static IntPtr FunctionIdMapper2([In] FunctionId functionId, [In] IntPtr clientData, [Out] bool* pbHookFunction)
        {
            var corProfiler = CorProfilerCallback.Instance;
            if (corProfiler == null)
            {
                Logger.LogWarning("Could not obtain CorProfilerCallback instance while mapping functions");
                return Unsafe.As<FunctionId, IntPtr>(ref functionId);
            }

            if (corProfiler.TryGetMethodHookEntry(functionId, out var methodData))
            {
                // Inject entry/exit hooks
                *pbHookFunction = true;
                return Unsafe.As<FunctionId, IntPtr>(ref functionId);
            }
            else
            {
                // Do not hook this function
                *pbHookFunction = false;
                return Unsafe.As<FunctionId, IntPtr>(ref functionId);
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void MethodEnterHook([In] IntPtr functionIdOrClientId, [In] COR_PRF_ELT_INFO eltInfo)
        {
            Console.WriteLine($"Entered method {functionIdOrClientId}");
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void MethodLeaveHook([In] IntPtr functionIdOrClientId, [In] COR_PRF_ELT_INFO eltInfo)
        {
            Console.WriteLine($"Exited method {functionIdOrClientId}");
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void MethodTailcallHook([In] IntPtr functionIdOrClientId, [In] COR_PRF_ELT_INFO eltInfo)
        {
            Console.WriteLine($"Tailcall method {functionIdOrClientId}");
        }
    }
}
