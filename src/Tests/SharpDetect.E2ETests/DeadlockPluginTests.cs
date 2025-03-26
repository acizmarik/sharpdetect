// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Cli.Handlers;
using SharpDetect.Core.Plugins;
using Xunit;

namespace SharpDetect.E2ETests;

[Collection("E2E")]
public class DeadlockPluginTests
{
    private const string ConfigurationFolder = "DeadlockPluginTestConfigurations";

    [Theory]
#if DEBUG
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_NoDeadlock)}_Debug.json")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_NoDeadlock)}_Release.json")]
#endif
    public async Task DeadlockPlugin_NoDeadlock(string configuration)
    {
        // Arrange
        var handler = RunCommandHandler.Create(configuration);
        var services = handler.ServiceProvider;
        var plugin = services.GetRequiredService<IPlugin>();
        var eventsDeliveryContext = services.GetRequiredService<IRecordedEventsDeliveryContext>();

        // Execute
        await handler.ExecuteAsync(null!);
        var report = plugin.CreateDiagnostics().GetAllReports().FirstOrDefault();

        // Assert
        Assert.Null(report);
        Assert.False(eventsDeliveryContext.HasBlockedThreads());
    }

    [Theory]
#if DEBUG
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_CanDetectDeadlock)}_Debug.json")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_CanDetectDeadlock)}_Release.json")]
#endif
    public async Task DeadlockPlugin_CanDetectDeadlock(string configuration)
    {
        // Arrange
        var handler = RunCommandHandler.Create(configuration);
        var services = handler.ServiceProvider;
        var plugin = services.GetRequiredService<IPlugin>();

        // Execute
        await handler.ExecuteAsync(null!);
        var report = plugin.CreateDiagnostics().GetAllReports().FirstOrDefault();

        // Assert
        Assert.NotNull(report);
        Assert.Equal(plugin.ReportCategory, report.Category);
    }
}
