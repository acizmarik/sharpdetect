// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Worker;
using Xunit;
using Xunit.Abstractions;

namespace SharpDetect.E2ETests;

[Collection("E2E")]
public class ShadowCallstackTests(ITestOutputHelper testOutput)
{
    private const string ConfigurationFolder = "ShadowCallstackTestConfigurations";

    [Theory]
#if DEBUG
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_Wait_ReentrancyWithPulse_Debug.json")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_Wait_ReentrancyWithPulse_Release.json")]
#endif
    public async Task ShadowCallstack_Monitor_Wait_ReentrancyWithPulse(string configuration)
    {
        // Arrange
        using var services = TestContextFactory.CreateServiceProvider(configuration, testOutput);
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();

        // Execute
        var exception = Record.ExceptionAsync(async () => await analysisWorker.ExecuteAsync(CancellationToken.None));

        // Assert
        Assert.Null(await exception);
    }
}

