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

[Collection("E2E")]
public class FieldAccessTests(ITestOutputHelper testOutput)
{
    private const string ConfigurationFolder = "FieldAccessTestConfigurations";

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(StaticField_ValueType_Read)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(StaticField_ValueType_Read)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(StaticField_ValueType_Read)}.json", "net10.0")]
    public Task StaticField_ValueType_Read(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.StaticFieldRead);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(StaticField_ValueType_Write)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(StaticField_ValueType_Write)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(StaticField_ValueType_Write)}.json", "net10.0")]
    public Task StaticField_ValueType_Write(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.StaticFieldWrite);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(StaticField_ReferenceType_Read)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(StaticField_ReferenceType_Read)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(StaticField_ReferenceType_Read)}.json", "net10.0")]
    public Task StaticField_ReferenceType_Read(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.StaticFieldRead);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(StaticField_ReferenceType_Write)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(StaticField_ReferenceType_Write)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(StaticField_ReferenceType_Write)}.json", "net10.0")]
    public Task StaticField_ReferenceType_Write(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.StaticFieldWrite);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ValueType_OnReferenceType_Read)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ValueType_OnReferenceType_Read)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ValueType_OnReferenceType_Read)}.json", "net10.0")]
    public Task InstanceField_ValueType_OnReferenceType_Read(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldRead);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ValueType_OnReferenceType_Write)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ValueType_OnReferenceType_Write)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ValueType_OnReferenceType_Write)}.json", "net10.0")]
    public Task InstanceField_ValueType_OnReferenceType_Write(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldWrite);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ReferenceType_OnReferenceType_Read)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ReferenceType_OnReferenceType_Read)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ReferenceType_OnReferenceType_Read)}.json", "net10.0")]
    public Task InstanceField_ReferenceType_OnReferenceType_Read(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldRead);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ReferenceType_OnReferenceType_Write)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ReferenceType_OnReferenceType_Write)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ReferenceType_OnReferenceType_Write)}.json", "net10.0")]
    public Task InstanceField_ReferenceType_OnReferenceType_Write(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldWrite);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ValueType_OnValueType_Read)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ValueType_OnValueType_Read)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ValueType_OnValueType_Read)}.json", "net10.0")]
    public Task InstanceField_ValueType_OnValueType_Read(string configuration, string sdk)
    {
        return FieldAccessNotInstrumented(configuration, sdk, RecordedEventType.InstanceFieldRead);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ValueType_OnValueType_Write)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ValueType_OnValueType_Write)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ValueType_OnValueType_Write)}.json", "net10.0")]
    public Task InstanceField_ValueType_OnValueType_Write(string configuration, string sdk)
    {
        return FieldAccessNotInstrumented(configuration, sdk, RecordedEventType.InstanceFieldWrite);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ReferenceType_OnValueType_Read)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ReferenceType_OnValueType_Read)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ReferenceType_OnValueType_Read)}.json", "net10.0")]
    public Task InstanceField_ReferenceType_OnValueType_Read(string configuration, string sdk)
    {
        return FieldAccessNotInstrumented(configuration, sdk, RecordedEventType.InstanceFieldRead);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ReferenceType_OnValueType_Write)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ReferenceType_OnValueType_Write)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_ReferenceType_OnValueType_Write)}.json", "net10.0")]
    public Task InstanceField_ReferenceType_OnValueType_Write(string configuration, string sdk)
    {
        return FieldAccessNotInstrumented(configuration, sdk, RecordedEventType.InstanceFieldWrite);
    }
    
    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ValueType_OnReferenceType_Read)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ValueType_OnReferenceType_Read)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ValueType_OnReferenceType_Read)}.json", "net10.0")]
    public Task InstanceField_Generic_FromType_ValueType_OnReferenceType_Read(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldRead);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ValueType_OnReferenceType_Write)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ValueType_OnReferenceType_Write)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ValueType_OnReferenceType_Write)}.json", "net10.0")]
    public Task InstanceField_Generic_FromType_ValueType_OnReferenceType_Write(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldWrite);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ReferenceType_OnReferenceType_Read)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ReferenceType_OnReferenceType_Read)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ReferenceType_OnReferenceType_Read)}.json", "net10.0")]
    public Task InstanceField_Generic_FromType_ReferenceType_OnReferenceType_Read(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldRead);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ReferenceType_OnReferenceType_Write)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ReferenceType_OnReferenceType_Write)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ReferenceType_OnReferenceType_Write)}.json", "net10.0")]
    public Task InstanceField_Generic_FromType_ReferenceType_OnReferenceType_Write(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldWrite);
    }
    
    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromMethod_ValueType_OnReferenceType_Read)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromMethod_ValueType_OnReferenceType_Read)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromMethod_ValueType_OnReferenceType_Read)}.json", "net10.0")]
    public Task InstanceField_Generic_FromMethod_ValueType_OnReferenceType_Read(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldRead);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromMethod_ValueType_OnReferenceType_Write)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromMethod_ValueType_OnReferenceType_Write)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromMethod_ValueType_OnReferenceType_Write)}.json", "net10.0")]
    public Task InstanceField_Generic_FromMethod_ValueType_OnReferenceType_Write(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldWrite);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromMethod_ReferenceType_OnReferenceType_Read)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromMethod_ReferenceType_OnReferenceType_Read)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromMethod_ReferenceType_OnReferenceType_Read)}.json", "net10.0")]
    public Task InstanceField_Generic_FromMethod_ReferenceType_OnReferenceType_Read(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldRead);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromMethod_ReferenceType_OnReferenceType_Write)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromMethod_ReferenceType_OnReferenceType_Write)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromMethod_ReferenceType_OnReferenceType_Write)}.json", "net10.0")]
    public Task InstanceField_Generic_FromMethod_ReferenceType_OnReferenceType_Write(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldWrite);
    }
    
    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromBoth_ValueType_OnReferenceType_Read)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromBoth_ValueType_OnReferenceType_Read)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromBoth_ValueType_OnReferenceType_Read)}.json", "net10.0")]
    public Task InstanceField_Generic_FromBoth_ValueType_OnReferenceType_Read(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldRead);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromBoth_ValueType_OnReferenceType_Write)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromBoth_ValueType_OnReferenceType_Write)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromBoth_ValueType_OnReferenceType_Write)}.json", "net10.0")]
    public Task InstanceField_Generic_FromBoth_ValueType_OnReferenceType_Write(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldWrite);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromBoth_ReferenceType_OnReferenceType_Read)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromBoth_ReferenceType_OnReferenceType_Read)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromBoth_ReferenceType_OnReferenceType_Read)}.json", "net10.0")]
    public Task InstanceField_Generic_FromBoth_ReferenceType_OnReferenceType_Read(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldRead);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromBoth_ReferenceType_OnReferenceType_Write)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromBoth_ReferenceType_OnReferenceType_Write)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromBoth_ReferenceType_OnReferenceType_Write)}.json", "net10.0")]
    public Task InstanceField_Generic_FromBoth_ReferenceType_OnReferenceType_Write(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldWrite);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ValueType_OnValueType_Read)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ValueType_OnValueType_Read)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ValueType_OnValueType_Read)}.json", "net10.0")]
    public Task InstanceField_Generic_FromType_ValueType_OnValueType_Read(string configuration, string sdk)
    {
        return FieldAccessNotInstrumented(configuration, sdk, RecordedEventType.InstanceFieldRead);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ValueType_OnValueType_Write)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ValueType_OnValueType_Write)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ValueType_OnValueType_Write)}.json", "net10.0")]
    public Task InstanceField_Generic_FromType_ValueType_OnValueType_Write(string configuration, string sdk)
    {
        return FieldAccessNotInstrumented(configuration, sdk, RecordedEventType.InstanceFieldWrite);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ReferenceType_OnValueType_Read)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ReferenceType_OnValueType_Read)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ReferenceType_OnValueType_Read)}.json", "net10.0")]
    public Task InstanceField_Generic_FromType_ReferenceType_OnValueType_Read(string configuration, string sdk)
    {
        return FieldAccessNotInstrumented(configuration, sdk, RecordedEventType.InstanceFieldRead);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ReferenceType_OnValueType_Write)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ReferenceType_OnValueType_Write)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ReferenceType_OnValueType_Write)}.json", "net10.0")]
    public Task InstanceField_Generic_FromType_ReferenceType_OnValueType_Write(string configuration, string sdk)
    {
        return FieldAccessNotInstrumented(configuration, sdk, RecordedEventType.InstanceFieldWrite);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ArrayOfValueType_OnReferenceType_Read)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ArrayOfValueType_OnReferenceType_Read)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ArrayOfValueType_OnReferenceType_Read)}.json", "net10.0")]
    public Task InstanceField_Generic_FromType_ArrayOfValueType_OnReferenceType_Read(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldRead);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ArrayOfValueType_OnReferenceType_Write)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ArrayOfValueType_OnReferenceType_Write)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ArrayOfValueType_OnReferenceType_Write)}.json", "net10.0")]
    public Task InstanceField_Generic_FromType_ArrayOfValueType_OnReferenceType_Write(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldWrite);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ArrayOfReferenceType_OnReferenceType_Read)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ArrayOfReferenceType_OnReferenceType_Read)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ArrayOfReferenceType_OnReferenceType_Read)}.json", "net10.0")]
    public Task InstanceField_Generic_FromType_ArrayOfReferenceType_OnReferenceType_Read(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldRead);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ArrayOfReferenceType_OnReferenceType_Write)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ArrayOfReferenceType_OnReferenceType_Write)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_ArrayOfReferenceType_OnReferenceType_Write)}.json", "net10.0")]
    public Task InstanceField_Generic_FromType_ArrayOfReferenceType_OnReferenceType_Write(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldWrite);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_NestedGeneric_ValueType_OnReferenceType_Read)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_NestedGeneric_ValueType_OnReferenceType_Read)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_NestedGeneric_ValueType_OnReferenceType_Read)}.json", "net10.0")]
    public Task InstanceField_Generic_FromType_NestedGeneric_ValueType_OnReferenceType_Read(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldRead);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_NestedGeneric_ValueType_OnReferenceType_Write)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_NestedGeneric_ValueType_OnReferenceType_Write)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_NestedGeneric_ValueType_OnReferenceType_Write)}.json", "net10.0")]
    public Task InstanceField_Generic_FromType_NestedGeneric_ValueType_OnReferenceType_Write(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldWrite);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_NestedGeneric_ReferenceType_OnReferenceType_Read)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_NestedGeneric_ReferenceType_OnReferenceType_Read)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_NestedGeneric_ReferenceType_OnReferenceType_Read)}.json", "net10.0")]
    public Task InstanceField_Generic_FromType_NestedGeneric_ReferenceType_OnReferenceType_Read(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldRead);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_NestedGeneric_ReferenceType_OnReferenceType_Write)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_NestedGeneric_ReferenceType_OnReferenceType_Write)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_FromType_NestedGeneric_ReferenceType_OnReferenceType_Write)}.json", "net10.0")]
    public Task InstanceField_Generic_FromType_NestedGeneric_ReferenceType_OnReferenceType_Write(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldWrite);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_MultiParam_ValueType_OnReferenceType_Read)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_MultiParam_ValueType_OnReferenceType_Read)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_MultiParam_ValueType_OnReferenceType_Read)}.json", "net10.0")]
    public Task InstanceField_Generic_MultiParam_ValueType_OnReferenceType_Read(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldRead);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_MultiParam_ValueType_OnReferenceType_Write)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_MultiParam_ValueType_OnReferenceType_Write)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_MultiParam_ValueType_OnReferenceType_Write)}.json", "net10.0")]
    public Task InstanceField_Generic_MultiParam_ValueType_OnReferenceType_Write(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldWrite);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_MultiParam_ReferenceType_OnReferenceType_Read)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_MultiParam_ReferenceType_OnReferenceType_Read)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_MultiParam_ReferenceType_OnReferenceType_Read)}.json", "net10.0")]
    public Task InstanceField_Generic_MultiParam_ReferenceType_OnReferenceType_Read(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldRead);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_MultiParam_ReferenceType_OnReferenceType_Write)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_MultiParam_ReferenceType_OnReferenceType_Write)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(InstanceField_Generic_MultiParam_ReferenceType_OnReferenceType_Write)}.json", "net10.0")]
    public Task InstanceField_Generic_MultiParam_ReferenceType_OnReferenceType_Write(string configuration, string sdk)
    {
        return FieldAccess(configuration, sdk, RecordedEventType.InstanceFieldWrite);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(VolatileStaticField_ValueType_Read)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(VolatileStaticField_ValueType_Read)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(VolatileStaticField_ValueType_Read)}.json", "net10.0")]
    public Task VolatileStaticField_ValueType_Read(string configuration, string sdk)
    {
        return VolatileFieldAccess(configuration, sdk, RecordedEventType.StaticFieldRead);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(VolatileStaticField_ValueType_Write)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(VolatileStaticField_ValueType_Write)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(VolatileStaticField_ValueType_Write)}.json", "net10.0")]
    public Task VolatileStaticField_ValueType_Write(string configuration, string sdk)
    {
        return VolatileFieldAccess(configuration, sdk, RecordedEventType.StaticFieldWrite);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(VolatileInstanceField_ValueType_Read)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(VolatileInstanceField_ValueType_Read)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(VolatileInstanceField_ValueType_Read)}.json", "net10.0")]
    public Task VolatileInstanceField_ValueType_Read(string configuration, string sdk)
    {
        return VolatileFieldAccess(configuration, sdk, RecordedEventType.InstanceFieldRead);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(VolatileInstanceField_ValueType_Write)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(VolatileInstanceField_ValueType_Write)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(VolatileInstanceField_ValueType_Write)}.json", "net10.0")]
    public Task VolatileInstanceField_ValueType_Write(string configuration, string sdk)
    {
        return VolatileFieldAccess(configuration, sdk, RecordedEventType.InstanceFieldWrite);
    }

    private async Task FieldAccess(string configuration, string sdk, RecordedEventType eventType)
    {
        // Arrange
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginAdditionalData, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestExecutionOrderingPlugin>();
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

    private async Task FieldAccessNotInstrumented(string configuration, string sdk, RecordedEventType eventType)
    {
        // Arrange
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginAdditionalData, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestExecutionOrderingPlugin>();
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

    private async Task VolatileFieldAccess(string configuration, string sdk, RecordedEventType eventType)
    {
        // Arrange
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginAdditionalData, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestExecutionOrderingPlugin>();
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

