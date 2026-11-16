// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Plugins.Descriptors;
using System.Reflection;

namespace SharpDetect.E2ETests.Utils;

internal static class TestMethodDescriptors
{
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
        return MethodDescriptor.ForStaticMethod(SubjectEntryType.FullName!, methodName)
            .InAssembly(SubjectEntryType.Assembly.GetName().Name!)
            .WithUninterpretedExit()
            .Build();
    }
}