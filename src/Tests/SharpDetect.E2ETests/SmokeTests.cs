// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common.Plugins;
using SharpDetect.Common.Services.Reporting;
using SharpDetect.TestUtils;
using SharpDetect.TestUtils.E2E;
using Xunit;

namespace SharpDetect.E2ETests
{
    [Collection("E2E")]
    public class SmokeTests
    {
        [Fact]
        public async Task SmokeTests_HelloWorld_RunsToCompletion()
        {
            // Prepare
            await using var session = SessionHelpers.CreateAnalysisSession(E2ETestsConfiguration.SubjectDllPath, "Reporter");

            // Act
            await session.Start();
            var analysisStarted = false;
            var analysisEnded = false;
            var reachedEntryPoint = false;
            var invalidProgramException = false;
            var reportsReader = session.GetRequiredService<IReportsReaderProvider>().GetReportsReader();
            await foreach (var report in reportsReader.ReadAllAsync())
            {
                if (report.Category == nameof(IPlugin.AnalysisStarted))
                    analysisStarted = true;
                else if (report.Category == nameof(IPlugin.AnalysisEnded))
                    analysisEnded = true;
                else if (report.Category == nameof(IPlugin.TypeLoaded))
                {
                    if (report.MessageFormat == typeof(InvalidProgramException).FullName)
                        invalidProgramException = true;
                }
                else if (report.Category == nameof(IPlugin.MethodCalled))
                {
                    if (report.MessageFormat == $"{typeof(void).FullName} {E2ETestsConfiguration.SubjectNamespace}.Program::Main(System.String[])")
                        reachedEntryPoint = true;
                }
            }

            // Assert
            Assert.False(invalidProgramException);
            Assert.True(analysisStarted);
            Assert.True(reachedEntryPoint);
            Assert.True(analysisEnded);
        }
    }
}