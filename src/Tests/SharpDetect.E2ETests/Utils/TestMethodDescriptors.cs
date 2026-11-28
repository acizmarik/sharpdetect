// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
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
        => AllTestMethods.Value.Concat(RuntimeAsyncHookedMethods.Value);

    private static readonly Lazy<MethodDescriptor[]> RuntimeAsyncHookedMethods = new(() =>
    [
        CreateHookedTaskReturningMethodDescriptor(
            "RuntimeAsyncHooked_TaskReturning",
            Sig.Class(TaskTypeName),
            Sig.Class(TaskTypeName)),
        CreateHookedTaskReturningMethodDescriptor(
            "RuntimeAsyncHooked_RuntimeAsync",
            Sig.Class(TaskTypeName),
            Sig.Class(TaskTypeName)),
        CreateHookedTaskReturningMethodDescriptor(
            "RuntimeAsyncHooked_GenericTaskReturning",
            Sig.Generic(TaskTResultTypeName, Sig.MethodTypeParameter(0)),
            Sig.MethodTypeParameter(0)),
    ]);

    private const string TaskTypeName = "System.Threading.Tasks.Task";
    private const string TaskTResultTypeName = "System.Threading.Tasks.Task`1";

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

    private static MethodDescriptor CreateHookedTaskReturningMethodDescriptor(
        string methodName,
        ArgumentTypeDescriptor returnType,
        ArgumentTypeDescriptor parameterType)
    {
        var isGeneric = parameterType.ElementTypes[0] == CorElementType.ELEMENT_TYPE_MVAR;
        return MethodDescriptor.ForStaticMethod(SubjectEntryType.FullName!, methodName)
            .InAssembly(SubjectEntryType.Assembly.GetName().Name!)
            .GenericParameters((byte)(isGeneric ? 1 : 0))
            .Parameters(parameterType)
            .Returns(returnType)
            .CaptureReturnValue(Capture.Reference)
            .WithUninterpretedExit()
            .Build();
    }
}