using SharpDetect.Common;
using SharpDetect.Common.LibraryDescriptors;

namespace SharpDetect.Core.Models.CoreLibrary
{
    internal partial class CoreLibDescriptor
    {
        public IEnumerable<(MethodIdentifier, MethodInterpretationData)> GetMonitorEnterExitMethods()
        {
            var declaringType = typeof(Monitor).FullName!;

            // System.Void System.Threading.Monitor::Enter(System.Object)
            yield return
            (
                new MethodIdentifier(
                    Name: nameof(Monitor.Enter),
                    DeclaringType: declaringType,
                    IsStatic: true,
                    ArgsCount: 1,
                    ArgumentTypes: new(new List<string>
                    {
                        typeof(object).FullName!
                    }),
                    IsInjected: false),
                new MethodInterpretationData(
                    Interpretation: MethodInterpretation.LockBlockingAcquire,
                    Flags: MethodRewritingFlags.InjectEntryExitHooks |
                            MethodRewritingFlags.CaptureArguments |
                            MethodRewritingFlags.InjectManagedWrapper,
                    CapturedParams: new CapturedParameterInfo[]
                    {
                        new(0, (ushort)UIntPtr.Size, false)
                    },
                    Checker: static (_, _) => true)
            );

            // System.Void System.Threading.Monitor::Enter(System.Object,System.Boolean&)
            yield return
            (
                new MethodIdentifier(
                    Name: nameof(Monitor.Enter),
                    DeclaringType: declaringType,
                    IsStatic: true,
                    ArgsCount: 2,
                    ArgumentTypes: new(new List<string>
                    {
                        typeof(object).FullName!,
                        typeof(bool).MakeByRefType().FullName!
                    }),
                    IsInjected: false),
                new MethodInterpretationData(
                    Interpretation: MethodInterpretation.LockBlockingAcquire,
                    Flags: MethodRewritingFlags.InjectEntryExitHooks |
                           MethodRewritingFlags.CaptureArguments |
                           MethodRewritingFlags.InjectManagedWrapper,
                    CapturedParams: new CapturedParameterInfo[]
                    {
                        new(0, (ushort)UIntPtr.Size, false),
                        new(1, sizeof(bool), true)
                    },
                    Checker: (_, byRefArgs) => (bool)byRefArgs[0].Argument.BoxedValue!)
            );

            // System.Void System.Threading.Monitor::ReliableEnterTimeout(System.Object,System.Int32,System.Boolean&)
            yield return
            (
                new MethodIdentifier(
                    Name: "ReliableEnterTimeout",
                    DeclaringType: declaringType,
                    IsStatic: true,
                    ArgsCount: 3,
                    ArgumentTypes: new(new List<string>
                    {
                        typeof(object).FullName!,
                        typeof(int).FullName!,
                        typeof(bool).MakeByRefType().FullName!,
                    }),
                    IsInjected: false),
                new MethodInterpretationData(
                    Interpretation: MethodInterpretation.LockTryAcquire,
                    Flags: MethodRewritingFlags.InjectEntryExitHooks |
                           MethodRewritingFlags.CaptureArguments |
                           MethodRewritingFlags.InjectManagedWrapper,
                    CapturedParams: new CapturedParameterInfo[]
                    {
                        new(0, (ushort)UIntPtr.Size, false),
                        new(1, sizeof(int), false),
                        new(2, sizeof(bool), true)
                    },
                    Checker: (_, byRefArgs) => (bool)byRefArgs[0].Argument.BoxedValue!)
            );

            // System.Void System.Threading.Monitor::Exit(System.Object)
            yield return
            (
                new MethodIdentifier(
                    Name: nameof(Monitor.Exit),
                    DeclaringType: declaringType,
                    IsStatic: true,
                    ArgsCount: 1,
                    ArgumentTypes: new(new List<string>
                    {
                        typeof(object).FullName!
                    }),
                    IsInjected: false),
                new MethodInterpretationData(
                    Interpretation: MethodInterpretation.LockRelease,
                    Flags: MethodRewritingFlags.InjectEntryExitHooks |
                           MethodRewritingFlags.CaptureArguments |
                           MethodRewritingFlags.InjectManagedWrapper,
                    CapturedParams: new CapturedParameterInfo[]
                    {
                        new(0, (ushort)UIntPtr.Size, false)
                    },
                    Checker: static (_, _) => true)
            );
        }
    }
}
