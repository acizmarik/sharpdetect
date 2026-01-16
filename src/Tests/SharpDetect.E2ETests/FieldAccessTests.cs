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
}

