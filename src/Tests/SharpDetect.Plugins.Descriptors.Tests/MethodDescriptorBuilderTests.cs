// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Plugins.Descriptors.Intrinsics;
using SharpDetect.Plugins.Descriptors.Methods;
using Xunit;

namespace SharpDetect.Plugins.Descriptors.Tests;

public class MethodDescriptorBuilderTests
{
    private const string TypeName = "Some.Type";

    [Fact]
    public void Build_WithNothingConfigured_UsesDefaults()
    {
        var descriptor = MethodDescriptor.ForStaticMethod(TypeName, "Method").Build();

        Assert.Equal("Method", descriptor.MethodName);
        Assert.Equal(TypeName, descriptor.DeclaringTypeFullName);
        Assert.Equal(DeclaringAssemblies.CoreLib, descriptor.DeclaringAssemblyName);
        Assert.Null(descriptor.VersionDescriptor);
        Assert.Equal([CorElementType.ELEMENT_TYPE_VOID], descriptor.SignatureDescriptor.ReturnType.ElementTypes);
        Assert.Equal(0, descriptor.SignatureDescriptor.ParametersCount);
        Assert.True(descriptor.RewritingDescriptor.InjectHooks);
        Assert.False(descriptor.RewritingDescriptor.InjectManagedWrapper);
        Assert.Empty(descriptor.RewritingDescriptor.Arguments);
        Assert.Null(descriptor.RewritingDescriptor.ReturnValue);
        Assert.Null(descriptor.RewritingDescriptor.MethodEnterInterpretation);
        Assert.Null(descriptor.RewritingDescriptor.MethodExitInterpretation);
        Assert.False(descriptor.RewritingDescriptor.EmitExitEvent);
    }

    [Fact]
    public void Build_DerivesSignatureFromMethodKindAndParameters()
    {
        var staticMethod = MethodDescriptor.ForStaticMethod(TypeName, "Method")
            .Parameters(Sig.Object, Sig.Int32)
            .Build();
        var instanceMethod = MethodDescriptor.ForInstanceMethod(TypeName, "Method")
            .Parameters(Sig.Object)
            .Build();
        var genericMethod = MethodDescriptor.ForStaticMethod(TypeName, "Method")
            .GenericParameters(2)
            .Build();

        Assert.Equal(CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT, staticMethod.SignatureDescriptor.CallingConvention);
        Assert.Equal(2, staticMethod.SignatureDescriptor.ParametersCount);
        Assert.Equal(CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS, instanceMethod.SignatureDescriptor.CallingConvention);
        Assert.Equal(1, instanceMethod.SignatureDescriptor.ParametersCount);
        Assert.Equal(
            CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT | CorCallingConvention.IMAGE_CEE_CS_CALLCONV_GENERIC,
            genericMethod.SignatureDescriptor.CallingConvention);
        Assert.Equal(2, genericMethod.SignatureDescriptor.GenericParametersCount);
    }

    [Fact]
    public void CaptureParameter_OnInstanceMethod_SkipsThis()
    {
        var descriptor = MethodDescriptor.ForInstanceMethod(TypeName, "Method")
            .Parameters(Sig.Int32, Sig.Object)
            .CaptureThis()
            .CaptureParameter(1, Capture.Reference)
            .Build();

        Assert.Equal(
            [new CapturedArgumentDescriptor(0, Capture.Reference), new CapturedArgumentDescriptor(2, Capture.Reference)],
            descriptor.RewritingDescriptor.Arguments);
    }

    [Fact]
    public void CaptureParameter_OnStaticMethod_UsesParameterPosition()
    {
        var descriptor = MethodDescriptor.ForStaticMethod(TypeName, "Method")
            .Parameters(Sig.Int32, Sig.Object)
            .CaptureParameter(1, Capture.Reference)
            .Build();

        Assert.Equal([new CapturedArgumentDescriptor(1, Capture.Reference)], descriptor.RewritingDescriptor.Arguments);
    }

    [Fact]
    public void OnEnter_Alone_ReportsNoExitEvent()
    {
        var descriptor = MethodDescriptor.ForStaticMethod(TypeName, "Method")
            .OnEnter(RecordedEventType.LockRelease)
            .Build();

        Assert.Equal((ushort)RecordedEventType.LockRelease, descriptor.RewritingDescriptor.MethodEnterInterpretation);
        Assert.Null(descriptor.RewritingDescriptor.MethodExitInterpretation);
        Assert.False(descriptor.RewritingDescriptor.EmitExitEvent);
    }

    [Fact]
    public void OnExit_ReportsInterpretedExitEvent()
    {
        var descriptor = MethodDescriptor.ForStaticMethod(TypeName, "Method")
            .OnExit(RecordedEventType.LockAcquireResult)
            .Build();

        Assert.Equal((ushort)RecordedEventType.LockAcquireResult, descriptor.RewritingDescriptor.MethodExitInterpretation);
        Assert.True(descriptor.RewritingDescriptor.EmitExitEvent);
    }

    [Fact]
    public void WithUninterpretedExit_ReportsExitEventWithoutInterpretation()
    {
        var descriptor = MethodDescriptor.ForStaticMethod(TypeName, "Method")
            .WithUninterpretedExit()
            .Build();

        Assert.Null(descriptor.RewritingDescriptor.MethodExitInterpretation);
        Assert.True(descriptor.RewritingDescriptor.EmitExitEvent);
    }

    [Fact]
    public void CaptureThis_OnStaticMethod_Throws()
    {
        var builder = MethodDescriptor.ForStaticMethod(TypeName, "Method");

        Assert.Throws<InvalidOperationException>(builder.CaptureThis);
    }

    [Fact]
    public void Build_CapturingUndeclaredParameter_Throws()
    {
        var builder = MethodDescriptor.ForInstanceMethod(TypeName, "Method")
            .Parameters(Sig.Object)
            .CaptureParameter(1, Capture.Reference);

        Assert.Throws<InvalidOperationException>(builder.Build);
    }

    [Fact]
    public void Build_CapturingSameArgumentTwice_Throws()
    {
        var builder = MethodDescriptor.ForStaticMethod(TypeName, "Method")
            .Parameters(Sig.Object)
            .CaptureParameter(0, Capture.Reference)
            .CaptureParameter(0, Capture.Reference);

        Assert.Throws<InvalidOperationException>(builder.Build);
    }

    [Fact]
    public void Build_DereferencingParameterNotPassedByReference_Throws()
    {
        var builder = MethodDescriptor.ForStaticMethod(TypeName, "Method")
            .Parameters(Sig.Boolean)
            .CaptureParameter(0, Capture.ByRefBoolean);

        Assert.Throws<InvalidOperationException>(builder.Build);
    }

    [Fact]
    public void Build_CapturingReturnValueWithoutExitEvent_Throws()
    {
        var builder = MethodDescriptor.ForStaticMethod(TypeName, "Method")
            .Returns(Sig.Boolean)
            .CaptureReturnValue(Capture.Boolean)
            .OnEnter(RecordedEventType.LockTryAcquire);

        Assert.Throws<InvalidOperationException>(builder.Build);
    }

    [Fact]
    public void Build_CapturingReturnValueOfVoidMethod_Throws()
    {
        var builder = MethodDescriptor.ForStaticMethod(TypeName, "Method")
            .CaptureReturnValue(Capture.Boolean)
            .OnExit(RecordedEventType.LockAcquireResult);

        Assert.Throws<InvalidOperationException>(builder.Build);
    }

    [Fact]
    public void ShippedDescriptors_AllBuild()
    {
        Assert.NotEmpty(SynchronizationMethodDescriptors.GetAllMethods());
        Assert.NotEmpty(FieldAccessDescriptors.GetAllMethods());
        Assert.NotEmpty(InterlockedIntrinsicDescriptors.GetAllIntrinsics());
        Assert.NotEmpty(VolatileIntrinsicDescriptors.GetAllIntrinsics());
    }
}
