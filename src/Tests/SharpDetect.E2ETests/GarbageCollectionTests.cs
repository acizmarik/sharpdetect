// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common.Plugins;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Services.Reporting;
using SharpDetect.E2ETests.Subject;
using SharpDetect.TestUtils;
using SharpDetect.TestUtils.E2E;
using System.Collections.Concurrent;
using Xunit;

namespace SharpDetect.E2ETests
{
    [Collection("E2E")]
    public class GarbageCollectionTests
    {
        [Theory]
        [InlineData(nameof(Program.Test_SingleGarbageCollection_NonCompacting_Simple), 1)]
        [InlineData(nameof(Program.Test_MultipleGarbageCollection_NonCompacting_Simple), 2)]
        [InlineData(nameof(Program.Test_SingleGarbageCollection_Compacting_Simple), 1)]
        [InlineData(nameof(Program.Test_MultipleGarbageCollection_Compacting_Simple), 2)]
        public async Task GarbageCollectionTests_Simple(string testName, int expectedGarbageCollectionsCount)
        {
            // Prepare
            await using var session = SessionHelpers.CreateAnalysisSession(E2ETestsConfiguration.SubjectDllPath, "Reporter", testName);

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
            var garbageCollectionsCount = 0;
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
                    else if (report.MessageFormat == $"{typeof(void).FullName} {E2ETestsConfiguration.SubjectNamespace}.Program::{testName}()")
                        reachedTestMethod = true;
                }
                else if (report.Category == nameof(IPlugin.MethodReturned))
                {
                    if (report.MessageFormat == $"{typeof(void).FullName} {E2ETestsConfiguration.SubjectNamespace}.Program::Main(System.String[])")
                        leftEntryPoint = true;
                    if (report.MessageFormat == $"{typeof(void).FullName} {E2ETestsConfiguration.SubjectNamespace}.Program::{testName}()")
                        leftTestMethod = true;
                }
                else if (report.Category == nameof(IPlugin.GarbageCollectionStarted))
                {
                    garbageCollectionsCount++;
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
            Assert.True(reachedEntryPoint);
            Assert.True(reachedTestMethod);
            Assert.True(garbageCollectionStarted);
            Assert.True(garbageCollectionFinished);
            Assert.Equal(expectedGarbageCollectionsCount, garbageCollectionsCount);
            Assert.True(leftTestMethod);
            Assert.True(leftEntryPoint);
            Assert.True(analysisEnded);
        }

        [Theory]
        [InlineData(nameof(Program.Test_SingleGarbageCollection_ObjectTracking_Simple), 2)]
        [InlineData(nameof(Program.Test_MultipleGarbageCollection_ObjectTracking_Simple), 3)]
        [InlineData(nameof(Program.Test_SingleGarbageCollection_ObjectTracking_MovedLockedObject), 2)]
        public async Task GarbageCollectionTests_ObjectTracking(string testName, int expectedLockAcquiresCount)
        {
            // Prepare
            await using var session = SessionHelpers.CreateAnalysisSession(E2ETestsConfiguration.SubjectDllPath, "Reporter", testName);

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
            var lockObjects = new ConcurrentDictionary<IShadowObject, int>();
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
                    else if (report.MessageFormat == $"{typeof(void).FullName} {E2ETestsConfiguration.SubjectNamespace}.Program::{testName}()")
                        reachedTestMethod = true;
                }
                else if (report.Category == nameof(IPlugin.MethodReturned))
                {
                    if (report.MessageFormat == $"{typeof(void).FullName} {E2ETestsConfiguration.SubjectNamespace}.Program::Main(System.String[])")
                        leftEntryPoint = true;
                    if (report.MessageFormat == $"{typeof(void).FullName} {E2ETestsConfiguration.SubjectNamespace}.Program::{testName}()")
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
                    lockObjects.AddOrUpdate((report.Arguments![0] as IShadowObject)!, 1, (_, val) => val + 1);
                }
            }

            // Assert
            Assert.False(invalidProgramException);
            Assert.True(analysisStarted);
            Assert.True(reachedEntryPoint);
            Assert.True(reachedTestMethod);
            Assert.True(garbageCollectionStarted);
            Assert.True(garbageCollectionFinished);
            Assert.True(leftTestMethod);
            Assert.True(leftEntryPoint);
            Assert.Single(lockObjects);
            Assert.Equal(expectedLockAcquiresCount, lockAcquiresCount);
            Assert.True(analysisEnded);
        }
    }
}
