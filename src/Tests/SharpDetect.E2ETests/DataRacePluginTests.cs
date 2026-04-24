// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Reporting;
using SharpDetect.E2ETests.Utils;
using SharpDetect.Worker;
using Xunit;
using Xunit.Abstractions;

namespace SharpDetect.E2ETests;

[Collection("E2E")]
public class DataRacePluginTests(ITestOutputHelper testOutput)
{
    private const string ConfigurationFolder = "DataRacePluginTestConfigurations";
    private const string EraserPluginFullTypeName = "SharpDetect.E2ETests.Utils.TestEraserPlugin";
    private const string FastTrackPluginFullTypeName = "SharpDetect.E2ETests.Utils.TestFastTrackPlugin";

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Static_ReadWriteRace)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Static_ReadWriteRace)}.json", "net8.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Static_ReadWriteRace)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Static_ReadWriteRace)}.json", "net9.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Static_ReadWriteRace)}.json", "net10.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Static_ReadWriteRace)}.json", "net10.0", EraserPluginFullTypeName)]
    public Task CanDetectDataRace_ReferenceType_Static_ReadWriteRace(string configuration, string sdk, string plugin)
    {
        return AssertDetectsDataRace(configuration, sdk, plugin);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Static_ReadWriteRace)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Static_ReadWriteRace)}.json", "net8.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Static_ReadWriteRace)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Static_ReadWriteRace)}.json", "net9.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Static_ReadWriteRace)}.json", "net10.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Static_ReadWriteRace)}.json", "net10.0", EraserPluginFullTypeName)]
    public Task CanDetectDataRace_ValueType_Static_ReadWriteRace(string configuration, string sdk, string plugin)
    {
        return AssertDetectsDataRace(configuration, sdk, plugin);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Static_WriteReadRace)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Static_WriteReadRace)}.json", "net8.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Static_WriteReadRace)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Static_WriteReadRace)}.json", "net9.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Static_WriteReadRace)}.json", "net10.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Static_WriteReadRace)}.json", "net10.0", EraserPluginFullTypeName)]
    public Task CanDetectDataRace_ReferenceType_Static_WriteReadRace(string configuration, string sdk, string plugin)
    {
        return AssertDetectsDataRace(configuration, sdk, plugin);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Static_WriteReadRace)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Static_WriteReadRace)}.json", "net8.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Static_WriteReadRace)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Static_WriteReadRace)}.json", "net9.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Static_WriteReadRace)}.json", "net10.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Static_WriteReadRace)}.json", "net10.0", EraserPluginFullTypeName)]
    public Task CanDetectDataRace_ValueType_Static_WriteReadRace(string configuration, string sdk, string plugin)
    {
        return AssertDetectsDataRace(configuration, sdk, plugin);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Instance_ReadWriteRace)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Instance_ReadWriteRace)}.json", "net8.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Instance_ReadWriteRace)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Instance_ReadWriteRace)}.json", "net9.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Instance_ReadWriteRace)}.json", "net10.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Instance_ReadWriteRace)}.json", "net10.0", EraserPluginFullTypeName)]
    public Task CanDetectDataRace_ReferenceType_Instance_ReadWriteRace(string configuration, string sdk, string plugin)
    {
        return AssertDetectsDataRace(configuration, sdk, plugin);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Instance_ReadWriteRace)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Instance_ReadWriteRace)}.json", "net8.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Instance_ReadWriteRace)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Instance_ReadWriteRace)}.json", "net9.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Instance_ReadWriteRace)}.json", "net10.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Instance_ReadWriteRace)}.json", "net10.0", EraserPluginFullTypeName)]
    public Task CanDetectDataRace_ValueType_Instance_ReadWriteRace(string configuration, string sdk, string plugin)
    {
        return AssertDetectsDataRace(configuration, sdk, plugin);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Instance_WriteReadRace)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Instance_WriteReadRace)}.json", "net8.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Instance_WriteReadRace)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Instance_WriteReadRace)}.json", "net9.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Instance_WriteReadRace)}.json", "net10.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Instance_WriteReadRace)}.json", "net10.0", EraserPluginFullTypeName)]
    public Task CanDetectDataRace_ReferenceType_Instance_WriteReadRace(string configuration, string sdk, string plugin)
    {
        return AssertDetectsDataRace(configuration, sdk, plugin);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Instance_WriteReadRace)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Instance_WriteReadRace)}.json", "net8.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Instance_WriteReadRace)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Instance_WriteReadRace)}.json", "net9.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Instance_WriteReadRace)}.json", "net10.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Instance_WriteReadRace)}.json", "net10.0", EraserPluginFullTypeName)]
    public Task CanDetectDataRace_ValueType_Instance_WriteReadRace(string configuration, string sdk, string plugin)
    {
        return AssertDetectsDataRace(configuration, sdk, plugin);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Instance_WriteWriteRace)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Instance_WriteWriteRace)}.json", "net8.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Instance_WriteWriteRace)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Instance_WriteWriteRace)}.json", "net9.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Instance_WriteWriteRace)}.json", "net10.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ReferenceType_Instance_WriteWriteRace)}.json", "net10.0", EraserPluginFullTypeName)]
    public Task CanDetectDataRace_ReferenceType_Instance_WriteWriteRace(string configuration, string sdk, string plugin)
    {
        return AssertDetectsDataRace(configuration, sdk, plugin);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Instance_WriteWriteRace)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Instance_WriteWriteRace)}.json", "net8.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Instance_WriteWriteRace)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Instance_WriteWriteRace)}.json", "net9.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Instance_WriteWriteRace)}.json", "net10.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanDetectDataRace_ValueType_Instance_WriteWriteRace)}.json", "net10.0", EraserPluginFullTypeName)]
    public Task CanDetectDataRace_ValueType_Instance_WriteWriteRace(string configuration, string sdk, string plugin)
    {
        return AssertDetectsDataRace(configuration, sdk, plugin);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ReferenceType_Static_ReadReadNoRace)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ReferenceType_Static_ReadReadNoRace)}.json", "net8.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ReferenceType_Static_ReadReadNoRace)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ReferenceType_Static_ReadReadNoRace)}.json", "net9.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ReferenceType_Static_ReadReadNoRace)}.json", "net10.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ReferenceType_Static_ReadReadNoRace)}.json", "net10.0", EraserPluginFullTypeName)]
    public Task NoDataRace_ReferenceType_Static_ReadReadNoRace(string configuration, string sdk, string plugin)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk, plugin);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ValueType_Static_ReadReadNoRace)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ValueType_Static_ReadReadNoRace)}.json", "net8.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ValueType_Static_ReadReadNoRace)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ValueType_Static_ReadReadNoRace)}.json", "net9.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ValueType_Static_ReadReadNoRace)}.json", "net10.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ValueType_Static_ReadReadNoRace)}.json", "net10.0", EraserPluginFullTypeName)]
    public Task NoDataRace_ValueType_Static_ReadReadNoRace(string configuration, string sdk, string plugin)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk, plugin);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ReferenceType_Instance_ReadReadNoRace)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ReferenceType_Instance_ReadReadNoRace)}.json", "net8.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ReferenceType_Instance_ReadReadNoRace)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ReferenceType_Instance_ReadReadNoRace)}.json", "net9.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ReferenceType_Instance_ReadReadNoRace)}.json", "net10.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ReferenceType_Instance_ReadReadNoRace)}.json", "net10.0", EraserPluginFullTypeName)]
    public Task NoDataRace_ReferenceType_Instance_ReadReadNoRace(string configuration, string sdk, string plugin)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk, plugin);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ValueType_Instance_ReadReadNoRace)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ValueType_Instance_ReadReadNoRace)}.json", "net8.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ValueType_Instance_ReadReadNoRace)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ValueType_Instance_ReadReadNoRace)}.json", "net9.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ValueType_Instance_ReadReadNoRace)}.json", "net10.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ValueType_Instance_ReadReadNoRace)}.json", "net10.0", EraserPluginFullTypeName)]
    public Task NoDataRace_ValueType_Instance_ReadReadNoRace(string configuration, string sdk, string plugin)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk, plugin);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ThreadStatic_ReferenceType)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ThreadStatic_ReferenceType)}.json", "net8.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ThreadStatic_ReferenceType)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ThreadStatic_ReferenceType)}.json", "net9.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ThreadStatic_ReferenceType)}.json", "net10.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ThreadStatic_ReferenceType)}.json", "net10.0", EraserPluginFullTypeName)]
    public Task NoDataRace_ThreadStatic_ReferenceType(string configuration, string sdk, string plugin)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk, plugin);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ThreadStatic_ValueType)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ThreadStatic_ValueType)}.json", "net8.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ThreadStatic_ValueType)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ThreadStatic_ValueType)}.json", "net9.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ThreadStatic_ValueType)}.json", "net10.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ThreadStatic_ValueType)}.json", "net10.0", EraserPluginFullTypeName)]
    public Task NoDataRace_ThreadStatic_ValueType(string configuration, string sdk, string plugin)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk, plugin);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ThreadStatic_ReadWrite)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ThreadStatic_ReadWrite)}.json", "net8.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ThreadStatic_ReadWrite)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ThreadStatic_ReadWrite)}.json", "net9.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ThreadStatic_ReadWrite)}.json", "net10.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_ThreadStatic_ReadWrite)}.json", "net10.0", EraserPluginFullTypeName)]
    public Task NoDataRace_ThreadStatic_ReadWrite(string configuration, string sdk, string plugin)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk, plugin);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileField_Static_ReadWriteNoRace)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileField_Static_ReadWriteNoRace)}.json", "net8.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileField_Static_ReadWriteNoRace)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileField_Static_ReadWriteNoRace)}.json", "net9.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileField_Static_ReadWriteNoRace)}.json", "net10.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileField_Static_ReadWriteNoRace)}.json", "net10.0", EraserPluginFullTypeName)]
    public Task NoDataRace_VolatileField_Static_ReadWriteNoRace(string configuration, string sdk, string plugin)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk, plugin);
    }
    
    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileField_Instance_ReadWriteNoRace)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileField_Instance_ReadWriteNoRace)}.json", "net8.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileField_Instance_ReadWriteNoRace)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileField_Instance_ReadWriteNoRace)}.json", "net9.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileField_Instance_ReadWriteNoRace)}.json", "net10.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileField_Instance_ReadWriteNoRace)}.json", "net10.0", EraserPluginFullTypeName)]
    public Task NoDataRace_VolatileField_Instance_ReadWriteNoRace(string configuration, string sdk, string plugin)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk, plugin);
    }
    
    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileExplicitAccess_Static_ReadWriteNoRace)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileExplicitAccess_Static_ReadWriteNoRace)}.json", "net8.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileExplicitAccess_Static_ReadWriteNoRace)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileExplicitAccess_Static_ReadWriteNoRace)}.json", "net9.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileExplicitAccess_Static_ReadWriteNoRace)}.json", "net10.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileExplicitAccess_Static_ReadWriteNoRace)}.json", "net10.0", EraserPluginFullTypeName)]
    public Task NoDataRace_VolatileExplicitAccess_Static_ReadWriteNoRace(string configuration, string sdk, string plugin)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk, plugin);
    }
    
    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileExplicitAccess_Instance_ReadWriteNoRace)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileExplicitAccess_Instance_ReadWriteNoRace)}.json", "net8.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileExplicitAccess_Instance_ReadWriteNoRace)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileExplicitAccess_Instance_ReadWriteNoRace)}.json", "net9.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileExplicitAccess_Instance_ReadWriteNoRace)}.json", "net10.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_VolatileExplicitAccess_Instance_ReadWriteNoRace)}.json", "net10.0", EraserPluginFullTypeName)]
    public Task NoDataRace_VolatileExplicitAccess_Instance_ReadWriteNoRace(string configuration, string sdk, string plugin)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk, plugin);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_Task_WriteInsideTask_ReadAfterTaskJoin)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_Task_WriteInsideTask_ReadAfterTaskJoin)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_Task_WriteInsideTask_ReadAfterTaskJoin)}.json", "net10.0", FastTrackPluginFullTypeName)]
    public Task NoDataRace_Task_WriteInsideTask_ReadAfterTaskJoin(string configuration, string sdk, string plugin)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk, plugin);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_Task_SequentialTasks_WriteRead)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_Task_SequentialTasks_WriteRead)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_Task_SequentialTasks_WriteRead)}.json", "net10.0", FastTrackPluginFullTypeName)]
    public Task NoDataRace_Task_SequentialTasks_WriteRead(string configuration, string sdk, string plugin)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk, plugin);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_SemaphoreSlim_ProtectedWriteRead)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_SemaphoreSlim_ProtectedWriteRead)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_SemaphoreSlim_ProtectedWriteRead)}.json", "net10.0", FastTrackPluginFullTypeName)]
    public Task NoDataRace_SemaphoreSlim_ProtectedWriteRead(string configuration, string sdk, string plugin)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk, plugin);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_GenericType_Static_DifferentInstantiations_WriteWrite_NoRace)}.json", "net8.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_GenericType_Static_DifferentInstantiations_WriteWrite_NoRace)}.json", "net8.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_GenericType_Static_DifferentInstantiations_WriteWrite_NoRace)}.json", "net9.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_GenericType_Static_DifferentInstantiations_WriteWrite_NoRace)}.json", "net9.0", EraserPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_GenericType_Static_DifferentInstantiations_WriteWrite_NoRace)}.json", "net10.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(NoDataRace_GenericType_Static_DifferentInstantiations_WriteWrite_NoRace)}.json", "net10.0", EraserPluginFullTypeName)]
    public Task NoDataRace_GenericType_Static_DifferentInstantiations_WriteWrite_NoRace(string configuration, string sdk, string plugin)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk, plugin);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(CanRenderReport)}.json", "net10.0", FastTrackPluginFullTypeName)]
    [InlineData($"{ConfigurationFolder}/{nameof(CanRenderReport)}.json", "net10.0", EraserPluginFullTypeName)]
    public async Task CanRenderReport(string configuration, string sdk, string pluginFullTypeName)
    {
        // Arrange
        var timeProvider = new TestTimeProvider(DateTimeOffset.MinValue);
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginFullTypeName, pluginAdditionalData, testOutput, timeProvider);
        var plugin = GetTestPlugin(services);
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var reportRenderer = services.GetRequiredService<IReportSummaryRenderer>();
        var reportWriter = services.GetRequiredService<IReportSummaryWriter>();

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        var summary = plugin.CreateDiagnostics();
        var context = new SummaryRenderingContext(summary, plugin, plugin.ReportTemplates, ConfigurationJson: string.Empty);
        var exception = await Record.ExceptionAsync(() => reportWriter.Write(context, reportRenderer, CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }
    
    private async Task AssertDetectsDataRace(
        string configuration,
        string sdk,
        string pluginTypeFullName)
    {
        // Arrange
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginTypeFullName, pluginAdditionalData, testOutput);
        var plugin = GetTestPlugin(services);
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        var subjectReports = plugin.GetSubjectReports().ToList();

        // Assert
        Assert.NotEmpty(subjectReports);
        var report = subjectReports.First();
        Assert.Equal(plugin.ReportCategory, report.Category);
    }

    private async Task AssertDoesNotDetectDataRace(string configuration, string sdk, string pluginTypeFullName)
    {
        // Arrange
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginTypeFullName, pluginAdditionalData, testOutput);
        var plugin = GetTestPlugin(services);
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        var subjectReports = plugin.GetSubjectReports().ToList();

        // Assert
        Assert.Empty(subjectReports);
    }

    private ITestPlugin GetTestPlugin(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService<TestEraserPlugin>() as ITestPlugin 
            ?? serviceProvider.GetService<TestFastTrackPlugin>()
            ?? throw new InvalidOperationException($"Plugin must be either {nameof(TestEraserPlugin)} or {nameof(TestFastTrackPlugin)}.");
    }
}
