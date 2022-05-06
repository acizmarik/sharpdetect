using SharpDetect.Common;
using SharpDetect.Common.LibraryDescriptors;

namespace SharpDetect.Core.Models.CoreLibrary
{
    internal partial class CoreLibDescriptor
    {
        public IEnumerable<(MethodIdentifier, MethodInterpretationData)> GetMonitorWaitPulseMethods()
        {
            var declaringType = typeof(Monitor).FullName!;

            // System.Boolean System.Threading.Monitor::Wait(System.Object)
            yield return
            (
                new MethodIdentifier(
                    Name: nameof(Monitor.Wait),
                    DeclaringType: declaringType,
                    IsStatic: true,
                    ArgsCount: 1,
                    ArgumentTypes: new(new List<string>
                    {
                        typeof(object).FullName!
                    })
                ),
                new MethodInterpretationData(
                    Interpretation: MethodInterpretation.SignalBlockingWait,
                    Flags: MethodRewritingFlags.InjectEntryExitHooks |
                           MethodRewritingFlags.CaptureArguments |
                           MethodRewritingFlags.CaptureReturnValue,
                    CapturedParams: new CapturedParameterInfo[]
                    {
                        new(0, (ushort)UIntPtr.Size, false)
                    },
                    Checker: static (retValue, _) => (bool)retValue.BoxedValue)
            );

            // System.Boolean System.Threading.Monitor::Wait(System.Object,System.TimeSpan)
            yield return
            (
                new MethodIdentifier(
                    Name: nameof(Monitor.Wait),
                    DeclaringType: declaringType,
                    IsStatic: true,
                    ArgsCount: 2,
                    ArgumentTypes: new(new List<string>
                    {
                        typeof(object).FullName!,
                        typeof(TimeSpan).FullName!
                    })),
                new MethodInterpretationData(
                    Interpretation: MethodInterpretation.SignalTryWait,
                    Flags: MethodRewritingFlags.InjectEntryExitHooks |
                           MethodRewritingFlags.CaptureArguments |
                           MethodRewritingFlags.CaptureReturnValue,
                    CapturedParams: new CapturedParameterInfo[]
                    {
                        new(0, (ushort)UIntPtr.Size, false)
                    },
                    Checker: static (retValue, _) => (bool)retValue.BoxedValue)
            );

            // System.Boolean System.Threading.Monitor::Wait(System.Object,System.Int32)
            yield return
            (
                new MethodIdentifier(
                    Name: nameof(Monitor.Wait),
                    DeclaringType: declaringType,
                    IsStatic: true,
                    ArgsCount: 2,
                    ArgumentTypes: new(new List<string>
                    {
                        typeof(object).FullName!,
                        typeof(int).FullName!
                    })),
                new MethodInterpretationData(
                    Interpretation: MethodInterpretation.SignalTryWait,
                    Flags: MethodRewritingFlags.InjectEntryExitHooks |
                           MethodRewritingFlags.CaptureArguments |
                           MethodRewritingFlags.CaptureReturnValue,
                    CapturedParams: new CapturedParameterInfo[]
                    {
                        new(0, (ushort)UIntPtr.Size, false)
                    },
                    Checker: static (retValue, _) => (bool)retValue.BoxedValue)
            );

            // System.Boolean System.Threading.Monitor::Wait(System.Object,System.Int32,System.Boolean)
            yield return
            (
                new MethodIdentifier(
                    Name: nameof(Monitor.Wait),
                    DeclaringType: declaringType,
                    IsStatic: true,
                    ArgsCount: 3,
                    ArgumentTypes: new(new List<string>
                    {
                        typeof(object).FullName!,
                        typeof(int).FullName!,
                        typeof(bool).FullName!
                    })),
                new MethodInterpretationData(
                    Interpretation: MethodInterpretation.SignalTryWait,
                    Flags: MethodRewritingFlags.InjectEntryExitHooks |
                           MethodRewritingFlags.CaptureArguments |
                           MethodRewritingFlags.CaptureReturnValue,
                    CapturedParams: new CapturedParameterInfo[]
                    {
                        new(0, (ushort)UIntPtr.Size, false)
                    },
                    Checker: static (retValue, _) => (bool)retValue.BoxedValue)
            );

            // System.Void System.Threading.Monitor::Pulse(System.Object)
            yield return
            (
                new MethodIdentifier(
                    Name: nameof(Monitor.Pulse),
                    DeclaringType: declaringType,
                    IsStatic: true,
                    ArgsCount: 1,
                    ArgumentTypes: new(new List<string>
                    {
                        typeof(object).FullName!
                    })),
                new MethodInterpretationData(
                    Interpretation: MethodInterpretation.SignalPulseOne,
                    Flags: MethodRewritingFlags.InjectEntryExitHooks |
                           MethodRewritingFlags.CaptureArguments,
                    CapturedParams: new CapturedParameterInfo[]
                    {
                        new(0, (ushort)UIntPtr.Size, false)
                    },
                    Checker: static (_, _) => true)
            );

            // System.Void System.Threading.Monitor::PulseAll(System.Object)
            yield return
            (
                new MethodIdentifier(
                    Name: nameof(Monitor.PulseAll),
                    DeclaringType: declaringType,
                    IsStatic: true,
                    ArgsCount: 1,
                    ArgumentTypes: new(new List<string>
                    {
                        typeof(object).FullName!
                    })),
                new MethodInterpretationData(
                    Interpretation: MethodInterpretation.SignalPulseAll,
                    Flags: MethodRewritingFlags.InjectEntryExitHooks |
                           MethodRewritingFlags.CaptureArguments,
                    CapturedParams: new CapturedParameterInfo[]
                    {
                        new(0, (ushort)UIntPtr.Size, false)
                    },
                    Checker: static (_, _) => true)
            );
        }
    }
}
