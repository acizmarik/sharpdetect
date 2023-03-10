using SharpDetect.Common.Plugins;
using SharpDetect.Common.Services.Reporting;
using SharpDetect.E2ETests.Definitions;
using SharpDetect.E2ETests.Subject;
using SharpDetect.E2ETests.Utilities;
using Xunit;

namespace SharpDetect.E2ETests
{
    [Collection("E2E")]
    public class MethodInterpretationTests
    {
        [Theory]
        [InlineData(nameof(Program.Test_MonitorMethods_EnterExit1))]
        [InlineData(nameof(Program.Test_MonitorMethods_EnterExit2))]
        [InlineData(nameof(Program.Test_MonitorMethods_TryEnterExit1))]
        [InlineData(nameof(Program.Test_MonitorMethods_TryEnterExit2))]
        [InlineData(nameof(Program.Test_MonitorMethods_TryEnterExit3))]
        public async Task MethodInterpretationTests_LockAttemptAcquireRelease(string testName)
        {
            // Prepare
            await using var session = SessionHelpers.CreateAnalysisSession(TestsConfiguration.SubjectDllPath, "Reporter", testName);

            // Act
            await session.Start();
            var analysisStarted = false;
            var analysisEnded = false;
            var reachedEntryPoint = false;
            var leftEntryPoint = false;
            var reachedTestMethod = false;
            var leftTestMethod = false;
            var invalidProgramException = false;
            var lockObjectAttempted = false;
            var lockedObject = false;
            var unlockedObject = false;
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
                    if (report.MessageFormat == $"{typeof(void).FullName} {TestsConfiguration.SubjectNamespace}.Program::Main(System.String[])")
                        reachedEntryPoint = true;
                    else if (report.MessageFormat == $"{typeof(void).FullName} {TestsConfiguration.SubjectNamespace}.Program::{testName}()")
                        reachedTestMethod = true;
                }
                else if (report.Category == nameof(IPlugin.MethodReturned))
                {
                    if (report.MessageFormat == $"{typeof(void).FullName} {TestsConfiguration.SubjectNamespace}.Program::Main(System.String[])")
                        leftEntryPoint = true;
                    if (report.MessageFormat == $"{typeof(void).FullName} {TestsConfiguration.SubjectNamespace}.Program::{testName}()")
                        leftTestMethod = true;
                }
                else if (report.Category == nameof(IPlugin.LockAcquireAttempted))
                {
                    if (reachedTestMethod && !leftTestMethod)
                        lockObjectAttempted = true;
                }
                else if (report.Category == nameof(IPlugin.LockAcquireReturned) && reachedTestMethod && !leftTestMethod)
                {
                    lockedObject = true;
                }
                else if (report.Category == nameof(IPlugin.LockReleased) && reachedTestMethod && !leftTestMethod)
                {
                    unlockedObject = true;
                }
            }

            // Assert
            Assert.False(invalidProgramException);
            Assert.True(analysisStarted);
            Assert.True(reachedEntryPoint);
            Assert.True(reachedTestMethod);
            Assert.True(lockObjectAttempted);
            Assert.True(lockedObject);
            Assert.True(unlockedObject);
            Assert.True(leftTestMethod);
            Assert.True(leftEntryPoint);
            Assert.True(analysisEnded);
        }
    }
}
