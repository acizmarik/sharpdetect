using SharpDetect.Common;
using SharpDetect.Common.Plugins;
using SharpDetect.Common.Services.Reporting;
using SharpDetect.E2ETests.Utilities;
using Xunit;

namespace SharpDetect.E2ETests
{
    public class SmokeTests
    {
        [Fact]
        public async Task SmokeTests_HelloWorld_RunsToCompletion()
        {
            // Prepare
            await CompilationHelpers.CompileTestAsync(TestsConfiguration.SubjectCsprojPath);
            await using var session = SessionHelpers.CreateAnalysisSession(TestsConfiguration.SubjectDllPath, "Reporter");

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
                    if (report.Description == typeof(InvalidProgramException).FullName)
                        invalidProgramException = true;
                }
                else if (report.Category == nameof(IPlugin.MethodCalled))
                {
                    if (report.Description == $"{typeof(void).FullName} {TestsConfiguration.SubjectNamespace}.Program::Main()")
                        reachedEntryPoint = true;
                }
            }

            // Assert
            Assert.True(analysisStarted);
            Assert.True(analysisEnded);
            Assert.True(reachedEntryPoint);
            Assert.False(invalidProgramException);
        }
    }
}