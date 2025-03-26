// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Cli.Handlers;
using SharpDetect.Core.Plugins;
using SharpDetect.E2ETests.Subject.Helpers;
using Xunit;

namespace SharpDetect.E2ETests;

[Collection("E2E")]
public class DisposablesPluginTests
{
    private const string ConfigurationFolder = "DisposablesPluginTestConfigurations";

    [Theory]
#if DEBUG
    [InlineData($"{ConfigurationFolder}/{nameof(DisposablesPlugin_CanDetectNonDisposed_CustomObject)}_Debug.json")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/{nameof(DisposablesPlugin_CanDetectNonDisposed_CustomObject)}_Release.json"))]
#endif
    public async Task DisposablesPlugin_CanDetectNonDisposed_CustomObject(string configuration)
    {
        // Arrange
        var handler = RunCommandHandler.Create(configuration);
        var services = handler.ServiceProvider;
        var plugin = services.GetRequiredService<IPlugin>();

        // Execute
        await handler.ExecuteAsync(null!);
        var reports = plugin.CreateDiagnostics().GetAllReports();

        // Assert
        Assert.NotEmpty(reports);
        Assert.NotNull(reports.FirstOrDefault(r =>
        {
            return r.Category == plugin.ReportCategory &&
                   r.Description.Contains("SharpDetect.E2ETests.Subject.Helpers.Disposables/CustomDisposable");
        }));
    }

    [Theory]
#if DEBUG
    [InlineData($"{ConfigurationFolder}/{nameof(DisposablesPlugin_CanDetectDisposed_CustomObject)}_Debug.json")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/{nameof(DisposablesPlugin_CanDetectDisposed_CustomObject)}_Release.json"))]
#endif
    public async Task DisposablesPlugin_CanDetectDisposed_CustomObject(string configuration)
    {
        // Arrange
        var handler = RunCommandHandler.Create(configuration);
        var services = handler.ServiceProvider;
        var plugin = services.GetRequiredService<IPlugin>();

        // Execute
        await handler.ExecuteAsync(null!);
        var reports = plugin.CreateDiagnostics().GetAllReports();

        // Assert
        Assert.Null(reports.FirstOrDefault(r =>
        {
            return r.Category == plugin.ReportCategory &&
                   r.Description.Contains("SharpDetect.E2ETests.Subject.Helpers.Disposables/CustomDisposable");
        }));
    }
}
