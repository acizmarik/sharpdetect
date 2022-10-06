using SharpDetect.Common.Plugins;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Services.Reporting;
using SharpDetect.E2ETests.Definitions;
using SharpDetect.E2ETests.Subject;
using SharpDetect.E2ETests.Utilities;
using Xunit;

namespace SharpDetect.E2ETests
{
    [Collection("E2E")]
    public class GarbageCollectionTests
    {
        [Theory]
        [InlineData(nameof(Program.Test_GarbageCollection_Simple))]
        public async Task GarbageCollectionTests_Simple(string testName)
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
            var garbageCollectionStarted = false;
            var garbageCollectionFinished = false;
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
                else if (report.Category == nameof(IPlugin.GarbageCollectionStarted))
                {
                    garbageCollectionStarted = true;
                }
                else if (report.Category == nameof(IPlugin.GarbageCollectionFinished))
                {
                    garbageCollectionFinished = true;
                }
            }

            // Assert
            Assert.False(invalidProgramException);
            Assert.True(analysisStarted);
            Assert.True(analysisEnded);
            Assert.True(reachedEntryPoint);
            Assert.True(reachedTestMethod);
            Assert.True(garbageCollectionStarted);
            Assert.True(garbageCollectionFinished);
            Assert.True(leftTestMethod);
            Assert.True(leftEntryPoint);
        }

        [Theory]
        [InlineData(nameof(Program.Test_GarbageCollection_ObjectTracking))]
        public async Task GarbageCollectionTests_ObjectTracking(string testName)
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
            var garbageCollectionStarted = false;
            var garbageCollectionFinished = false;
            var lockAcquiresCount = 0;
            var lockObjects = new HashSet<IShadowObject>();
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
                else if (report.Category == nameof(IPlugin.GarbageCollectionStarted))
                {
                    garbageCollectionStarted = true;
                }
                else if (report.Category == nameof(IPlugin.GarbageCollectionFinished))
                {
                    garbageCollectionFinished = true;
                }
                else if (report.Category == nameof(IPlugin.LockAcquireAttempted) && reachedTestMethod && !leftTestMethod)
                {
                    lockAcquiresCount++;
                    lockObjects.Add((report.Arguments![0] as IShadowObject)!);
                }
            }

            // Assert
            Assert.False(invalidProgramException);
            Assert.True(analysisStarted);
            Assert.True(analysisEnded);
            Assert.True(reachedEntryPoint);
            Assert.True(reachedTestMethod);
            Assert.True(garbageCollectionStarted);
            Assert.True(garbageCollectionFinished);
            Assert.True(leftTestMethod);
            Assert.True(leftEntryPoint);
            Assert.Single(lockObjects);
            Assert.Equal(2, lockAcquiresCount);
        }
    }
}
