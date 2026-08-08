// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Plugins.Descriptors;
using System.Reflection;

namespace SharpDetect.E2ETests.Utils;

internal static class TestMethodDescriptors
{
    private static readonly MethodSignatureDescriptor VoidMethodNoArgsSignature = new (
        CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
        ParametersCount: 0,
        ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
        ArgumentTypeElements: []);

    private static readonly MethodRewritingDescriptor InjectHooksRewritingDescriptor = new(
        InjectHooks: true,
        InjectManagedWrapper: false,
        Arguments: [],
        ReturnValue: null,
        MethodEnterInterpretation: null,
        MethodExitInterpretation: null);
    
    private const string TestMethodPrefix = "Test_";
    private static readonly Type SubjectEntryType = typeof(Subject.Program);

    private static readonly Lazy<MethodDescriptor[]> AllTestMethods = new(() =>
        [.. GetSubjectEntryMethodNames().Select(CreateHookedEntryMethodDescriptor)]);

    public static IEnumerable<MethodDescriptor> GetAllTestMethods()
        => AllTestMethods.Value;

    private static IEnumerable<string> GetSubjectEntryMethodNames()
    {
        return SubjectEntryType
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => m.Name.StartsWith(TestMethodPrefix, StringComparison.Ordinal))
            .Where(m => m.ReturnType == typeof(void) && m.GetParameters().Length == 0 && !m.IsGenericMethodDefinition)
            .Select(m => m.Name)
            .Distinct(StringComparer.Ordinal);
    }

    private static MethodDescriptor CreateHookedEntryMethodDescriptor(string methodName)
    {
        return new MethodDescriptor(
            MethodName: methodName,
            DeclaringTypeFullName: SubjectEntryType.FullName!,
            VersionDescriptor: null,
            VoidMethodNoArgsSignature,
            InjectHooksRewritingDescriptor);
    }
}