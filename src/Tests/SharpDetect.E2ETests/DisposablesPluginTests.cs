// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Plugins;
using SharpDetect.Plugins.Disposables;
using SharpDetect.Worker;
using Xunit;
using Xunit.Abstractions;

namespace SharpDetect.E2ETests;

[Collection("E2E")]
public class DisposablesPluginTests(ITestOutputHelper testOutput)
{
    private const string ConfigurationFolder = "DisposablesPluginTestConfigurations";

    [Theory]
#if DEBUG
    [InlineData($"{ConfigurationFolder}/{nameof(DisposablesPlugin_CanDetectNonDisposed_CustomObject)}_Debug.json")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/{nameof(DisposablesPlugin_CanDetectNonDisposed_CustomObject)}_Release.json")]
#endif
    public async Task DisposablesPlugin_CanDetectNonDisposed_CustomObject(string configuration)
    {
        // Arrange
        var services = TestContextFactory.CreateServiceProvider(configuration, testOutput);
        var plugin = services.GetRequiredService<IPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
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
    [InlineData($"{ConfigurationFolder}/{nameof(DisposablesPlugin_CanDetectDisposed_CustomObject)}_Release.json")]
#endif
    public async Task DisposablesPlugin_CanDetectDisposed_CustomObject(string configuration)
    {
        // Arrange
        var services = TestContextFactory.CreateServiceProvider(configuration, testOutput);
        var plugin = services.GetRequiredService<IPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        var reports = plugin.CreateDiagnostics().GetAllReports();

        // Assert
        Assert.Null(reports.FirstOrDefault(r =>
        {
            return r.Category == plugin.ReportCategory &&
                   r.Description.Contains("SharpDetect.E2ETests.Subject.Helpers.Disposables/CustomDisposable");
        }));
    }
}
