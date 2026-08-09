// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Events;
using SharpDetect.Core.Plugins;
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
    private const string SubjectAssemblyName = "SharpDetect.E2ETests.Subject";
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

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicStaticField_Increment(string sdk)
        => FieldAccess(
            "Test_Field_Atomic_ValueType_Static_Increment",
            sdk,
            RecordedEventType.StaticFieldRead,
            FieldAccessKind.Atomic);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicStaticField_CompareExchange(string sdk)
        => FieldAccess(
            "Test_Field_Atomic_ValueType_Static_CompareExchange",
            sdk,
            RecordedEventType.StaticFieldRead,
            FieldAccessKind.Atomic);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicStaticField_CompareExchange_ComputedArguments(string sdk)
        => FieldAccess(
            "Test_Field_Atomic_ValueType_Static_CompareExchangeComputedArguments",
            sdk,
            RecordedEventType.StaticFieldRead,
            FieldAccessKind.Atomic);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicLocal_Increment_NotInstrumented(string sdk)
        => NoFieldAccessOfKind(
            "Test_Field_Atomic_ValueType_Local_Increment",
            sdk,
            RecordedEventType.StaticFieldRead,
            FieldAccessKind.Atomic);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task VolatileCallStaticField_Read(string sdk)
        => FieldAccess(
            "Test_Field_VolatileCall_ValueType_Static_Read",
            sdk,
            RecordedEventType.StaticFieldRead,
            FieldAccessKind.Volatile);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task VolatileCallStaticField_Write(string sdk)
        => FieldAccess(
            "Test_Field_VolatileCall_ValueType_Static_Write",
            sdk,
            RecordedEventType.StaticFieldWrite,
            FieldAccessKind.Volatile);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicStaticField_Read_IsAcquireOnly(string sdk)
        => FieldAccess(
            "Test_Field_Atomic_ValueType_Static_Read",
            sdk,
            RecordedEventType.StaticFieldRead,
            FieldAccessKind.Volatile);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicStaticField_ReferenceType_CompareExchange(string sdk)
        => FieldAccess(
            "Test_Field_Atomic_ReferenceType_Static_CompareExchange",
            sdk,
            RecordedEventType.StaticFieldRead,
            FieldAccessKind.Atomic);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicInstanceField_ReferenceType_CompareExchange(string sdk)
        => FieldAccess(
            "Test_Field_Atomic_ReferenceType_Instance_CompareExchange",
            sdk,
            RecordedEventType.InstanceFieldRead,
            FieldAccessKind.Atomic);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task VolatileCallStaticField_ReferenceType_Read(string sdk)
        => FieldAccess(
            "Test_Field_VolatileCall_ReferenceType_Static_Read",
            sdk,
            RecordedEventType.StaticFieldRead,
            FieldAccessKind.Volatile);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task VolatileCallStaticField_ReferenceType_Write(string sdk)
        => FieldAccess(
            "Test_Field_VolatileCall_ReferenceType_Static_Write",
            sdk,
            RecordedEventType.StaticFieldWrite,
            FieldAccessKind.Volatile);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicInstanceField_Increment(string sdk)
        => FieldAccess(
            "Test_Field_Atomic_ValueType_Instance_Increment",
            sdk,
            RecordedEventType.InstanceFieldRead,
            FieldAccessKind.Atomic);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicInstanceField_CompareExchange(string sdk)
        => FieldAccess(
            "Test_Field_Atomic_ValueType_Instance_CompareExchange",
            sdk,
            RecordedEventType.InstanceFieldRead,
            FieldAccessKind.Atomic);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicInstanceField_OnValueType_NotInstrumented(string sdk)
        => NoFieldAccessOfKind(
            "Test_Field_Atomic_ValueType_OnValueType_Instance_Increment",
            sdk,
            RecordedEventType.InstanceFieldRead,
            FieldAccessKind.Atomic);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task VolatileCallInstanceField_Read(string sdk)
        => FieldAccess(
            "Test_Field_VolatileCall_ValueType_Instance_Read",
            sdk,
            RecordedEventType.InstanceFieldRead,
            FieldAccessKind.Volatile);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task VolatileCallInstanceField_Write(string sdk)
        => FieldAccess(
            "Test_Field_VolatileCall_ValueType_Instance_Write",
            sdk,
            RecordedEventType.InstanceFieldWrite,
            FieldAccessKind.Volatile);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task StaticField_ValueType_TernaryWrite_BranchTargetsStore(string sdk)
        => FieldAccess("Test_Field_ValueType_Static_TernaryWrite", sdk, RecordedEventType.StaticFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task VolatileStaticField_ValueType_TernaryWrite_BranchTargetsStore(string sdk)
        => VolatileFieldAccess("Test_Field_Volatile_ValueType_Static_TernaryWrite", sdk, RecordedEventType.StaticFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_ReferenceType_TernaryValueWrite_BranchTargetsStore(string sdk)
        => FieldAccess("Test_Field_ReferenceType_Instance_TernaryValueWrite", sdk, RecordedEventType.InstanceFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_ReferenceType_TernaryReceiverRead_BranchTargetsLoad(string sdk)
        => FieldAccess("Test_Field_ReferenceType_Instance_TernaryReceiverRead", sdk, RecordedEventType.InstanceFieldRead);

    private async Task FieldAccess(
        string subjectArgs,
        string sdk,
        RecordedEventType eventType,
        FieldAccessKind? requireAccessKind = null)
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
            .Then(EventuallyFieldAccessInAssembly(SubjectAssemblyName, eventType, plugin, requireAccessKind))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }

    private async Task NoFieldAccessOfKind(
        string subjectArgs,
        string sdk,
        RecordedEventType eventType,
        FieldAccessKind accessKind)
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
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
        Assert.DoesNotContain(events, evt => evt.Type == eventType && eventType switch
        {
            RecordedEventType.StaticFieldRead => evt.Get<(RecordedEventMetadata, StaticFieldReadArgs)>().Item2.AccessKind,
            RecordedEventType.StaticFieldWrite => evt.Get<(RecordedEventMetadata, StaticFieldWriteArgs)>().Item2.AccessKind,
            RecordedEventType.InstanceFieldRead => evt.Get<(RecordedEventMetadata, InstanceFieldReadArgs)>().Item2.AccessKind,
            RecordedEventType.InstanceFieldWrite => evt.Get<(RecordedEventMetadata, InstanceFieldWriteArgs)>().Item2.AccessKind,
            _ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, "Not a field access event.")
        } == accessKind);
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
            .Then(EventuallyFieldAccessInAssembly(SubjectAssemblyName, eventType, plugin, requireAccessKind: FieldAccessKind.Volatile))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }
}
