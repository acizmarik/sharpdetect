// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Events;
using SharpDetect.E2ETests.Utils;
using SharpDetect.TemporalAsserts;
using SharpDetect.TemporalAsserts.TemporalOperators;
using SharpDetect.Worker;
using SharpDetect.Worker.Commands.Run;
using Xunit;
using Xunit.Abstractions;
using static SharpDetect.E2ETests.TemporalAssertionBuilders;

namespace SharpDetect.E2ETests;

[Collection(CollectionName)]
public class FieldAccessTests(ITestOutputHelper testOutput)
{
    public const string CollectionName = "E2E_FieldAccessTests";
    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task StaticField_ValueType_Read(string sdk)
        => FieldAccess("Test_Field_ValueType_Static_Read", sdk, RecordedEventType.StaticFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task StaticField_ValueType_Write(string sdk)
        => FieldAccess("Test_Field_ValueType_Static_Write", sdk, RecordedEventType.StaticFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task StaticField_ReferenceType_Read(string sdk)
        => FieldAccess("Test_Field_ReferenceType_Static_Read", sdk, RecordedEventType.StaticFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task StaticField_ReferenceType_Write(string sdk)
        => FieldAccess("Test_Field_ReferenceType_Static_Write", sdk, RecordedEventType.StaticFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_ValueType_OnReferenceType_Read(string sdk)
        => FieldAccess("Test_Field_ValueType_Instance_Read", sdk, RecordedEventType.InstanceFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_ValueType_OnReferenceType_Write(string sdk)
        => FieldAccess("Test_Field_ValueType_Instance_Write", sdk, RecordedEventType.InstanceFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_ReferenceType_OnReferenceType_Read(string sdk)
        => FieldAccess("Test_Field_ReferenceType_Instance_Read", sdk, RecordedEventType.InstanceFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_ReferenceType_OnReferenceType_Write(string sdk)
        => FieldAccess("Test_Field_ReferenceType_Instance_Write", sdk, RecordedEventType.InstanceFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_ValueType_OnValueType_Read(string sdk)
        => FieldAccessNotInstrumented("Test_Field_ValueType_OnValueType_Instance_Read", sdk, RecordedEventType.InstanceFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_ValueType_OnValueType_Write(string sdk)
        => FieldAccessNotInstrumented("Test_Field_ValueType_OnValueType_Instance_Write", sdk, RecordedEventType.InstanceFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_ReferenceType_OnValueType_Read(string sdk)
        => FieldAccessNotInstrumented("Test_Field_ReferenceType_OnValueType_Instance_Read", sdk, RecordedEventType.InstanceFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_ReferenceType_OnValueType_Write(string sdk)
        => FieldAccessNotInstrumented("Test_Field_ReferenceType_OnValueType_Instance_Write", sdk, RecordedEventType.InstanceFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromType_ValueType_OnReferenceType_Read(string sdk)
        => FieldAccess("Test_Field_Generic_FromType_ValueType_OnReferenceType_Instance_Read", sdk, RecordedEventType.InstanceFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromType_ValueType_OnReferenceType_Write(string sdk)
        => FieldAccess("Test_Field_Generic_FromType_ValueType_OnReferenceType_Instance_Write", sdk, RecordedEventType.InstanceFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromType_ReferenceType_OnReferenceType_Read(string sdk)
        => FieldAccess("Test_Field_Generic_FromType_ReferenceType_OnReferenceType_Instance_Read", sdk, RecordedEventType.InstanceFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromType_ReferenceType_OnReferenceType_Write(string sdk)
        => FieldAccess("Test_Field_Generic_FromType_ReferenceType_OnReferenceType_Instance_Write", sdk, RecordedEventType.InstanceFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromMethod_ValueType_OnReferenceType_Read(string sdk)
        => FieldAccess("Test_Field_Generic_FromMethod_ValueType_OnReferenceType_Instance_Read", sdk, RecordedEventType.InstanceFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromMethod_ValueType_OnReferenceType_Write(string sdk)
        => FieldAccess("Test_Field_Generic_FromMethod_ValueType_OnReferenceType_Instance_Write", sdk, RecordedEventType.InstanceFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromMethod_ReferenceType_OnReferenceType_Read(string sdk)
        => FieldAccess("Test_Field_Generic_FromMethod_ReferenceType_OnReferenceType_Instance_Read", sdk, RecordedEventType.InstanceFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromMethod_ReferenceType_OnReferenceType_Write(string sdk)
        => FieldAccess("Test_Field_Generic_FromMethod_ReferenceType_OnReferenceType_Instance_Write", sdk, RecordedEventType.InstanceFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromBoth_ValueType_OnReferenceType_Read(string sdk)
        => FieldAccess("Test_Field_Generic_FromBoth_ValueType_OnReferenceType_Instance_Read", sdk, RecordedEventType.InstanceFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromBoth_ValueType_OnReferenceType_Write(string sdk)
        => FieldAccess("Test_Field_Generic_FromBoth_ValueType_OnReferenceType_Instance_Write", sdk, RecordedEventType.InstanceFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromBoth_ReferenceType_OnReferenceType_Read(string sdk)
        => FieldAccess("Test_Field_Generic_FromBoth_ReferenceType_OnReferenceType_Instance_Read", sdk, RecordedEventType.InstanceFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromBoth_ReferenceType_OnReferenceType_Write(string sdk)
        => FieldAccess("Test_Field_Generic_FromBoth_ReferenceType_OnReferenceType_Instance_Write", sdk, RecordedEventType.InstanceFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromType_ValueType_OnValueType_Read(string sdk)
        => FieldAccessNotInstrumented("Test_Field_Generic_FromType_ValueType_OnValueType_Instance_Read", sdk, RecordedEventType.InstanceFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromType_ValueType_OnValueType_Write(string sdk)
        => FieldAccessNotInstrumented("Test_Field_Generic_FromType_ValueType_OnValueType_Instance_Write", sdk, RecordedEventType.InstanceFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromType_ReferenceType_OnValueType_Read(string sdk)
        => FieldAccessNotInstrumented("Test_Field_Generic_FromType_ReferenceType_OnValueType_Instance_Read", sdk, RecordedEventType.InstanceFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromType_ReferenceType_OnValueType_Write(string sdk)
        => FieldAccessNotInstrumented("Test_Field_Generic_FromType_ReferenceType_OnValueType_Instance_Write", sdk, RecordedEventType.InstanceFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromType_ArrayOfValueType_OnReferenceType_Read(string sdk)
        => FieldAccess("Test_Field_Generic_FromType_ArrayOfValueType_OnReferenceType_Instance_Read", sdk, RecordedEventType.InstanceFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromType_ArrayOfValueType_OnReferenceType_Write(string sdk)
        => FieldAccess("Test_Field_Generic_FromType_ArrayOfValueType_OnReferenceType_Instance_Write", sdk, RecordedEventType.InstanceFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromType_ArrayOfReferenceType_OnReferenceType_Read(string sdk)
        => FieldAccess("Test_Field_Generic_FromType_ArrayOfReferenceType_OnReferenceType_Instance_Read", sdk, RecordedEventType.InstanceFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromType_ArrayOfReferenceType_OnReferenceType_Write(string sdk)
        => FieldAccess("Test_Field_Generic_FromType_ArrayOfReferenceType_OnReferenceType_Instance_Write", sdk, RecordedEventType.InstanceFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromType_NestedGeneric_ValueType_OnReferenceType_Read(string sdk)
        => FieldAccess("Test_Field_Generic_FromType_NestedGeneric_ValueType_OnReferenceType_Instance_Read", sdk, RecordedEventType.InstanceFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromType_NestedGeneric_ValueType_OnReferenceType_Write(string sdk)
        => FieldAccess("Test_Field_Generic_FromType_NestedGeneric_ValueType_OnReferenceType_Instance_Write", sdk, RecordedEventType.InstanceFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromType_NestedGeneric_ReferenceType_OnReferenceType_Read(string sdk)
        => FieldAccess("Test_Field_Generic_FromType_NestedGeneric_ReferenceType_OnReferenceType_Instance_Read", sdk, RecordedEventType.InstanceFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_FromType_NestedGeneric_ReferenceType_OnReferenceType_Write(string sdk)
        => FieldAccess("Test_Field_Generic_FromType_NestedGeneric_ReferenceType_OnReferenceType_Instance_Write", sdk, RecordedEventType.InstanceFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_MultiParam_ValueType_OnReferenceType_Read(string sdk)
        => FieldAccess("Test_Field_Generic_MultiParam_ValueType_OnReferenceType_Instance_Read", sdk, RecordedEventType.InstanceFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_MultiParam_ValueType_OnReferenceType_Write(string sdk)
        => FieldAccess("Test_Field_Generic_MultiParam_ValueType_OnReferenceType_Instance_Write", sdk, RecordedEventType.InstanceFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_MultiParam_ReferenceType_OnReferenceType_Read(string sdk)
        => FieldAccess("Test_Field_Generic_MultiParam_ReferenceType_OnReferenceType_Instance_Read", sdk, RecordedEventType.InstanceFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_Generic_MultiParam_ReferenceType_OnReferenceType_Write(string sdk)
        => FieldAccess("Test_Field_Generic_MultiParam_ReferenceType_OnReferenceType_Instance_Write", sdk, RecordedEventType.InstanceFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task VolatileStaticField_ValueType_Read(string sdk)
        => VolatileFieldAccess("Test_Field_Volatile_ValueType_Static_Read", sdk, RecordedEventType.StaticFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task VolatileStaticField_ValueType_Write(string sdk)
        => VolatileFieldAccess("Test_Field_Volatile_ValueType_Static_Write", sdk, RecordedEventType.StaticFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task VolatileInstanceField_ValueType_Read(string sdk)
        => VolatileFieldAccess("Test_Field_Volatile_ValueType_Instance_Read", sdk, RecordedEventType.InstanceFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task VolatileInstanceField_ValueType_Write(string sdk)
        => VolatileFieldAccess("Test_Field_Volatile_ValueType_Instance_Write", sdk, RecordedEventType.InstanceFieldWrite);

    private async Task FieldAccess(string subjectArgs, string sdk, RecordedEventType eventType)
    {
        // Arrange
        var additionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput, additionalData);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args!, plugin)
            .Then(EventuallyEventType(eventType))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }

    private async Task FieldAccessNotInstrumented(string subjectArgs, string sdk, RecordedEventType eventType)
    {
        // Arrange
        var additionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput, additionalData);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);

        // Verify: method enters and exits successfully (no crash from skipped instrumentation)
        var assert = EventuallyMethodEnter(args.Target.Args!, plugin)
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }

    private async Task VolatileFieldAccess(string subjectArgs, string sdk, RecordedEventType eventType)
    {
        // Arrange
        var additionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput, additionalData);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args!, plugin)
            .Then(EventuallyVolatileFieldAccess(eventType))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }
}
