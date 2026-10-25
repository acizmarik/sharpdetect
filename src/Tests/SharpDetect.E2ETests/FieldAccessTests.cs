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
    private const string IlSubjectAssemblyName = "SharpDetect.E2ETests.Subject.IL";
    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task StaticField_ValueType_Read(string sdk)
        => FieldAccess("Test_Field_ValueType_Static_Read", sdk, RecordedEventType.StaticFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task StaticField_Readonly_Read_IsInstrumented(string sdk)
        => FieldAccess("Test_TypeInitializer_Completes", sdk, RecordedEventType.StaticFieldRead);

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
        => FieldAccessSequence(
            "Test_Field_Atomic_ValueType_Static_Increment",
            sdk,
            (RecordedEventType.StaticFieldWrite, FieldAccessKind.Atomic),
            (RecordedEventType.StaticFieldRead, FieldAccessKind.Atomic));

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicStaticField_CompareExchange(string sdk)
        => FieldAccessSequence(
            "Test_Field_Atomic_ValueType_Static_CompareExchange",
            sdk,
            (RecordedEventType.StaticFieldWrite, FieldAccessKind.CompareExchange),
            (RecordedEventType.StaticFieldRead, FieldAccessKind.CompareExchangeSucceeded));

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicStaticField_CompareExchange_ComputedArguments(string sdk)
        => FieldAccessSequence(
            "Test_Field_Atomic_ValueType_Static_CompareExchangeComputedArguments",
            sdk,
            (RecordedEventType.StaticFieldWrite, FieldAccessKind.CompareExchange),
            (RecordedEventType.StaticFieldRead, null));

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
        => FieldAccessSequence(
            "Test_Field_Atomic_ReferenceType_Static_CompareExchange",
            sdk,
            (RecordedEventType.StaticFieldWrite, FieldAccessKind.CompareExchange),
            (RecordedEventType.StaticFieldRead, FieldAccessKind.CompareExchangeSucceeded));

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicInstanceField_ReferenceType_CompareExchange(string sdk)
        => FieldAccessSequence(
            "Test_Field_Atomic_ReferenceType_Instance_CompareExchange",
            sdk,
            (RecordedEventType.InstanceFieldWrite, FieldAccessKind.CompareExchange),
            (RecordedEventType.InstanceFieldRead, FieldAccessKind.CompareExchangeSucceeded));

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
        => FieldAccessSequence(
            "Test_Field_Atomic_ValueType_Instance_Increment",
            sdk,
            (RecordedEventType.InstanceFieldWrite, FieldAccessKind.Atomic),
            (RecordedEventType.InstanceFieldRead, FieldAccessKind.Atomic));

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicInstanceField_CompareExchange(string sdk)
        => FieldAccessSequence(
            "Test_Field_Atomic_ValueType_Instance_CompareExchange",
            sdk,
            (RecordedEventType.InstanceFieldWrite, FieldAccessKind.CompareExchange),
            (RecordedEventType.InstanceFieldRead, FieldAccessKind.CompareExchangeSucceeded));

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicStaticField_Exchange_ReleasesAfterValueArgument(string sdk)
        => FieldAccessSequence(
            "Test_Field_Atomic_ExchangeReleasesAfterValueArgument",
            sdk,
            (RecordedEventType.StaticFieldWrite, FieldAccessKind.Regular),
            (RecordedEventType.StaticFieldWrite, FieldAccessKind.Atomic),
            (RecordedEventType.StaticFieldRead, FieldAccessKind.Atomic));

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicStaticField_CompareExchange_ReportsFailureAfterTheCall(string sdk)
        => FieldAccessSequence(
            "Test_Field_Atomic_CompareExchangeFails",
            sdk,
            (RecordedEventType.StaticFieldWrite, FieldAccessKind.CompareExchange),
            (RecordedEventType.StaticFieldRead, FieldAccessKind.CompareExchangeFailed));

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicStaticField_CompareExchange_ComparandThroughBranch(string sdk)
        => FieldAccessSequence(
            "Test_Field_Atomic_CompareExchangeComparandThroughBranch",
            sdk,
            (RecordedEventType.StaticFieldWrite, FieldAccessKind.CompareExchange),
            (RecordedEventType.StaticFieldRead, FieldAccessKind.CompareExchangeSucceeded));

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicStaticField_Exchange_ConditionalValue(string sdk)
        => FieldAccessSequence(
            "Test_Field_Atomic_ExchangeConditionalValue",
            sdk,
            (RecordedEventType.StaticFieldWrite, FieldAccessKind.Atomic),
            (RecordedEventType.StaticFieldRead, FieldAccessKind.Atomic));

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicInstanceField_Exchange_ConditionalValue(string sdk)
        => FieldAccessSequence(
            "Test_Field_Atomic_ExchangeConditionalValue_Instance",
            sdk,
            (RecordedEventType.InstanceFieldWrite, FieldAccessKind.Atomic),
            (RecordedEventType.InstanceFieldRead, FieldAccessKind.Atomic));

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicStaticField_Increment_ConditionalLocation_NotInstrumented(string sdk)
        => NoFieldAccessOfKind(
            "Test_Field_Atomic_IncrementConditionalLocation",
            sdk,
            RecordedEventType.StaticFieldWrite,
            FieldAccessKind.Atomic);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicInstanceField_CompareExchange_OfGenericParameter(string sdk)
        => FieldAccessSequence(
            "Test_Field_Atomic_CompareExchangeOfGenericParameter",
            sdk,
            (RecordedEventType.InstanceFieldWrite, FieldAccessKind.CompareExchange),
            (RecordedEventType.InstanceFieldRead, FieldAccessKind.CompareExchangeSucceeded));

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicInstanceField_CompareExchange_OfBaseClassConstrainedParameter_ReportsFailure(string sdk)
        => FieldAccessSequence(
            "Test_Field_Atomic_CompareExchangeOfBaseClassConstrainedParameter",
            sdk,
            (RecordedEventType.InstanceFieldWrite, FieldAccessKind.CompareExchange),
            (RecordedEventType.InstanceFieldRead, FieldAccessKind.CompareExchangeFailed));

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicInstanceField_CompareExchange_OfGenericBaseClassConstrainedParameter_ReportsFailure(string sdk)
        => FieldAccessSequence(
            "Test_Field_Atomic_CompareExchangeOfGenericBaseClassConstrainedParameter",
            sdk,
            (RecordedEventType.InstanceFieldWrite, FieldAccessKind.CompareExchange),
            (RecordedEventType.InstanceFieldRead, FieldAccessKind.CompareExchangeFailed));

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicInstanceField_CompareExchange_OfForeignBaseClassConstrainedParameter_FallsBackToReadModifyWrite(string sdk)
        => FieldAccessSequence(
            "Test_Field_Atomic_CompareExchangeOfForeignBaseClassConstrainedParameter",
            sdk,
            (RecordedEventType.InstanceFieldWrite, FieldAccessKind.Atomic),
            (RecordedEventType.InstanceFieldRead, FieldAccessKind.Atomic));

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicStaticField_CompareExchange_OfDoubleNaN_ReportsSuccess(string sdk)
        => FieldAccessSequence(
            "Test_Field_Atomic_CompareExchangeOfDoubleNaN",
            sdk,
            (RecordedEventType.StaticFieldWrite, FieldAccessKind.CompareExchange),
            (RecordedEventType.StaticFieldRead, FieldAccessKind.CompareExchangeSucceeded));

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicStaticField_CompareExchange_OfDoubleNegativeZero_ReportsFailure(string sdk)
        => FieldAccessSequence(
            "Test_Field_Atomic_CompareExchangeOfDoubleNegativeZero",
            sdk,
            (RecordedEventType.StaticFieldWrite, FieldAccessKind.CompareExchange),
            (RecordedEventType.StaticFieldRead, FieldAccessKind.CompareExchangeFailed));

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicStaticField_CompareExchange_OfSingleNaN_ReportsSuccess(string sdk)
        => FieldAccessSequence(
            "Test_Field_Atomic_CompareExchangeOfSingleNaN",
            sdk,
            (RecordedEventType.StaticFieldWrite, FieldAccessKind.CompareExchange),
            (RecordedEventType.StaticFieldRead, FieldAccessKind.CompareExchangeSucceeded));

    [Theory]
    [MemberData(nameof(SdkVersions.Net9AndAbove), MemberType = typeof(SdkVersions))]
    public Task AtomicStaticField_CompareExchange_OfStruct_FallsBackToReadModifyWrite(string sdk)
        => FieldAccessSequence(
            "Test_Field_Atomic_CompareExchangeOfStruct",
            sdk,
            (RecordedEventType.StaticFieldWrite, FieldAccessKind.Atomic));

    // The exchange throws inside the call, so an acquire reported after the call never happens
    [Theory]
    [MemberData(nameof(SdkVersions.Net9AndAbove), MemberType = typeof(SdkVersions))]
    public Task AtomicStaticField_ReadModifyWrite_ReportsNoAcquireWhenTheCallThrows(string sdk)
        => NoFieldAccessOfKind(
            "Test_Field_Atomic_CompareExchangeOfStruct",
            sdk,
            RecordedEventType.StaticFieldRead,
            FieldAccessKind.Atomic);

    [Theory]
    [MemberData(nameof(SdkVersions.Net9AndAbove), MemberType = typeof(SdkVersions))]
    public Task AtomicInstanceField_CompareExchange_OfUnconstrainedGenericParameter_FallsBackToReadModifyWrite(string sdk)
        => FieldAccessSequence(
            "Test_Field_Atomic_CompareExchangeOfUnconstrainedGenericParameter",
            sdk,
            (RecordedEventType.InstanceFieldWrite, FieldAccessKind.Atomic),
            (RecordedEventType.InstanceFieldRead, FieldAccessKind.Atomic));

    [Theory]
    [MemberData(nameof(SdkVersions.Net9AndAbove), MemberType = typeof(SdkVersions))]
    public Task AtomicStaticField_CompareExchange_OfEnum_FallsBackToReadModifyWrite(string sdk)
        => FieldAccessSequence(
            "Test_Field_Atomic_CompareExchangeOfEnum",
            sdk,
            (RecordedEventType.StaticFieldWrite, FieldAccessKind.Atomic),
            (RecordedEventType.StaticFieldRead, FieldAccessKind.Atomic));

    [Theory]
    [MemberData(nameof(SdkVersions.Net9AndAbove), MemberType = typeof(SdkVersions))]
    public Task AtomicStaticField_CompareExchange_OfByte(string sdk)
        => FieldAccessSequence(
            "Test_Field_Atomic_CompareExchangeOfByte",
            sdk,
            (RecordedEventType.StaticFieldWrite, FieldAccessKind.CompareExchange),
            (RecordedEventType.StaticFieldRead, FieldAccessKind.CompareExchangeSucceeded));

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

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_ReferenceType_OnValueType_InitOnlyObjectWrite(string sdk)
        => FieldAccessNotInstrumented("Test_Field_ReferenceType_OnValueType_InitOnlyObjectWrite", sdk, RecordedEventType.InstanceFieldWrite);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_ValueType_OnConstructedValueType_Read(string sdk)
        => FieldAccessNotInstrumented("Test_Field_ValueType_OnConstructedValueType_Instance_Read", sdk, RecordedEventType.InstanceFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_ValueType_OnConstructedGenericValueType_Read(string sdk)
        => FieldAccessNotInstrumented("Test_Field_ValueType_OnConstructedGenericValueType_Instance_Read", sdk, RecordedEventType.InstanceFieldRead);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_VolatileUnalignedPrefixes_Read(string sdk)
        => PrefixedFieldAccess("Test_Field_Prefixed_VolatileUnaligned_Instance_Read", sdk, RecordedEventType.InstanceFieldRead, FieldAccessKind.Volatile);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_UnalignedVolatilePrefixes_Read(string sdk)
        => PrefixedFieldAccess("Test_Field_Prefixed_UnalignedVolatile_Instance_Read", sdk, RecordedEventType.InstanceFieldRead, FieldAccessKind.Volatile);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_UnalignedPrefix_Read(string sdk)
        => PrefixedFieldAccess("Test_Field_Prefixed_Unaligned_Instance_Read", sdk, RecordedEventType.InstanceFieldRead, FieldAccessKind.Regular);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_VolatileUnalignedPrefixes_BranchTargetsFirstPrefix_Read(string sdk)
        => PrefixedFieldAccess("Test_Field_Prefixed_VolatileUnaligned_BranchTarget_Instance_Read", sdk, RecordedEventType.InstanceFieldRead, FieldAccessKind.Volatile);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_VolatileUnalignedPrefixes_Write(string sdk)
        => PrefixedFieldAccess("Test_Field_Prefixed_VolatileUnaligned_Instance_Write", sdk, RecordedEventType.InstanceFieldWrite, FieldAccessKind.Volatile);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_UnalignedVolatilePrefixes_Write(string sdk)
        => PrefixedFieldAccess("Test_Field_Prefixed_UnalignedVolatile_Instance_Write", sdk, RecordedEventType.InstanceFieldWrite, FieldAccessKind.Volatile);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_UnalignedPrefix_Write(string sdk)
        => PrefixedFieldAccess("Test_Field_Prefixed_Unaligned_Instance_Write", sdk, RecordedEventType.InstanceFieldWrite, FieldAccessKind.Regular);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task AtomicStaticField_Increment_InTailPosition(string sdk)
        => FieldAccessSequenceInAssembly(
            IlSubjectAssemblyName,
            "Test_Field_Prefixed_TailCall_Static_Increment",
            sdk,
            (RecordedEventType.StaticFieldWrite, FieldAccessKind.Atomic),
            (RecordedEventType.StaticFieldRead, FieldAccessKind.Atomic));

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task InstanceField_ValueType_OnForeignNewObj_Write(string sdk)
        => FieldAccess("Test_Field_ValueType_OnForeignNewObj_Instance_Write", sdk, RecordedEventType.InstanceFieldWrite);

    private Task PrefixedFieldAccess(string subjectArgs, string sdk, RecordedEventType eventType, FieldAccessKind accessKind)
        => FieldAccess(subjectArgs, sdk, eventType, accessKind, IlSubjectAssemblyName);

    private async Task FieldAccess(
        string subjectArgs,
        string sdk,
        RecordedEventType eventType,
        FieldAccessKind? requireAccessKind = null,
        string assemblyName = SubjectAssemblyName)
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
            .Then(EventuallyFieldAccessInAssembly(assemblyName, eventType, plugin, requireAccessKind))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }

    private Task FieldAccessSequence(
        string subjectArgs,
        string sdk,
        params (RecordedEventType EventType, FieldAccessKind? AccessKind)[] expectedAccesses)
        => FieldAccessSequenceInAssembly(SubjectAssemblyName, subjectArgs, sdk, expectedAccesses);

    private async Task FieldAccessSequenceInAssembly(
        string assemblyName,
        string subjectArgs,
        string sdk,
        params (RecordedEventType EventType, FieldAccessKind? AccessKind)[] expectedAccesses)
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
        AssertBase<ulong, RecordedEventType> assert = EventuallyMethodEnter(args.Target.Args!, plugin);
        foreach (var (eventType, accessKind) in expectedAccesses)
            assert = assert.Then(EventuallyFieldAccessInAssembly(assemblyName, eventType, plugin, accessKind));
        assert = assert.Then(EventuallyMethodExit(args.Target.Args!, plugin));

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
        var instrumented = EventuallyFieldAccessInMethod(args.Target.Args!, eventType, plugin);

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
        Assert.True(AssertStatus.Violated == instrumented.Evaluate(events), instrumented.GetDiagnosticInfo());
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
