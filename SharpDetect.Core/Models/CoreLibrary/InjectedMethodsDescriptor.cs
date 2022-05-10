using SharpDetect.Common;
using SharpDetect.Common.LibraryDescriptors;

namespace SharpDetect.Core.Models.CoreLibrary
{
    internal partial class CoreLibDescriptor
    {
        public IEnumerable<(MethodIdentifier, MethodInterpretationData)> GetInjectedMethods()
        {
            const string declaringType = "SharpDetect.EventDispatcher";

            // System.Void System.SharpDetect::FieldAccess(System.Boolean,System.UInt64)
            yield return
            (
                new MethodIdentifier(
                    Name: "FieldAccess",
                    DeclaringType: declaringType,
                    IsStatic: true,
                    ArgsCount: 2,
                    ArgumentTypes: new(new List<string>
                    {
                        typeof(bool).FullName!,
                        typeof(ulong).FullName!
                    }),
                    IsInjected: true
                ),
                new MethodInterpretationData(
                    Interpretation: MethodInterpretation.FieldAccess,
                    Flags: MethodRewritingFlags.InjectEntryExitHooks |
                           MethodRewritingFlags.CaptureArguments,
                    CapturedParams: new CapturedParameterInfo[]
                    {
                        new(0, sizeof(bool), false),
                        new(1, sizeof(ulong), false)
                    })
            );

            // System.Void System.SharpDetect::FieldInstanceAccess(System.Object)
            yield return
            (
                new MethodIdentifier(
                    Name: "FieldInstanceAccess",
                    DeclaringType: declaringType,
                    IsStatic: true,
                    ArgsCount: 1,
                    ArgumentTypes: new(new List<string>
                    {
                        typeof(object).FullName!
                    }),
                    IsInjected: true
                ),
                new MethodInterpretationData(
                    Interpretation: MethodInterpretation.FieldInstanceAccess,
                    Flags: MethodRewritingFlags.InjectEntryExitHooks |
                           MethodRewritingFlags.CaptureArguments,
                    CapturedParams: new CapturedParameterInfo[]
                    {
                        new(0, (ushort)UIntPtr.Size, false),
                    })
            );

            // System.Void System.SharpDetect::ArrayElementAccess(System.Boolean,System.UInt64)
            yield return
            (
                new MethodIdentifier(
                    Name: "ArrayElementAccess",
                    DeclaringType: declaringType,
                    IsStatic: true,
                    ArgsCount: 2,
                    ArgumentTypes: new(new List<string>
                    {
                        typeof(bool).FullName!,
                        typeof(ulong).FullName!,
                    }),
                    IsInjected: true
                ),
                new MethodInterpretationData(
                    Interpretation: MethodInterpretation.ArrayElementAccess,
                    Flags: MethodRewritingFlags.InjectEntryExitHooks |
                           MethodRewritingFlags.CaptureArguments,
                    CapturedParams: new CapturedParameterInfo[]
                    {
                        new(0, sizeof(bool), false),
                        new(1, sizeof(ulong), false)
                    })
            );

            // System.Void System.SharpDetect::ArrayInstanceAccess(System.Object)
            yield return
            (
                new MethodIdentifier(
                    Name: "ArrayInstanceAccess",
                    DeclaringType: declaringType,
                    IsStatic: true,
                    ArgsCount: 1,
                    ArgumentTypes: new(new List<string>
                    {
                        typeof(object).FullName!,
                    }),
                    IsInjected: true
                ),
                new MethodInterpretationData(
                    Interpretation: MethodInterpretation.ArrayInstanceAccess,
                    Flags: MethodRewritingFlags.InjectEntryExitHooks |
                           MethodRewritingFlags.CaptureArguments,
                    CapturedParams: new CapturedParameterInfo[]
                    {
                        new(0, (ushort)UIntPtr.Size, false),
                    })
            );

            // System.Void System.SharpDetect::ArrayIndexAccess(System.Int32)
            yield return
            (
                new MethodIdentifier(
                    Name: "ArrayIndexAccess",
                    DeclaringType: declaringType,
                    IsStatic: true,
                    ArgsCount: 1,
                    ArgumentTypes: new(new List<string>
                    {
                        typeof(int).FullName!,
                    }),
                    IsInjected: true
                ),
                new MethodInterpretationData(
                    Interpretation: MethodInterpretation.ArrayIndexAccess,
                    Flags: MethodRewritingFlags.InjectEntryExitHooks |
                           MethodRewritingFlags.CaptureArguments,
                    CapturedParams: new CapturedParameterInfo[]
                    {
                        new(0, sizeof(int), false),
                    })
            );
        }
    }
}
