﻿using dnlib.DotNet;
using SharpDetect.Common.Plugins;
using SharpDetect.Common.Services.Reporting;
using SharpDetect.E2ETests.Definitions;
using SharpDetect.E2ETests.Subject;
using SharpDetect.E2ETests.Utilities;
using SharpDetect.Plugins.LockSet;
using Xunit;

namespace SharpDetect.E2ETests
{
    [Collection("E2E")]
    public class DataRaceDetectionTests
    {
        [Theory]
        [InlineData(nameof(Program.Test_DataRace_ReferenceType_Static_SimpleRace), "Eraser")]
        [InlineData(nameof(Program.Test_DataRace_ReferenceType_Static_SimpleRace), "FastTrack")]
        [InlineData(nameof(Program.Test_DataRace_ReferenceType_Static_BadLocking), "Eraser")]
        [InlineData(nameof(Program.Test_DataRace_ReferenceType_Static_BadLocking), "FastTrack")]
        [InlineData(nameof(Program.Test_DataRace_ReferenceType_Instance_SimpleRace), "Eraser")]
        [InlineData(nameof(Program.Test_DataRace_ReferenceType_Instance_SimpleRace), "FastTrack")]
        [InlineData(nameof(Program.Test_DataRace_ReferenceType_Instance_BadLocking), "Eraser")]
        [InlineData(nameof(Program.Test_DataRace_ReferenceType_Instance_BadLocking), "FastTrack")]
        [InlineData(nameof(Program.Test_DataRace_ValueType_Static_SimpleRace), "Eraser")]
        [InlineData(nameof(Program.Test_DataRace_ValueType_Static_SimpleRace), "FastTrack")]
        [InlineData(nameof(Program.Test_DataRace_ValueType_Static_BadLocking), "Eraser")]
        [InlineData(nameof(Program.Test_DataRace_ValueType_Static_BadLocking), "FastTrack")]
        [InlineData(nameof(Program.Test_DataRace_ValueType_Instance_SimpleRace), "Eraser")]
        [InlineData(nameof(Program.Test_DataRace_ValueType_Instance_SimpleRace), "FastTrack")]
        [InlineData(nameof(Program.Test_DataRace_ValueType_Instance_BadLocking), "Eraser")]
        [InlineData(nameof(Program.Test_DataRace_ValueType_Instance_BadLocking), "FastTrack")]
        public async Task DataRaceDetectionTests_Fields_ShouldDetectDataRace(string testName, string plugin)
        {
            // Prepare
            await using var session = SessionHelpers.CreateAnalysisSession(TestsConfiguration.SubjectDllPath, $"{plugin}|Reporter", testName);
            var fieldName = testName.Substring(0, testName.LastIndexOf('_'));

            // Act
            await session.Start();
            var analysisStarted = false;
            var analysisEnded = false;
            var reachedEntryPoint = false;
            var leftEntryPoint = false;
            var reachedTestMethod = false;
            var leftTestMethod = false;
            var invalidProgramException = false;
            var dataRace = false;
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
                else if (report.Category == EraserPlugin.DiagnosticsCategory)
                {
                    var arguments = report.Arguments;
                    var field = (arguments![0] as FieldDef)!;
                    if (field.FullName.Contains(fieldName) && reachedTestMethod && !leftTestMethod)
                        dataRace = true;
                }
            }

            // Assert
            Assert.False(invalidProgramException);
            Assert.True(analysisStarted);
            Assert.True(analysisEnded);
            Assert.True(reachedEntryPoint);
            Assert.True(reachedTestMethod);
            Assert.True(dataRace);
            Assert.True(leftTestMethod);
            Assert.True(leftEntryPoint);
        }

        [Theory]
        [InlineData(nameof(Program.Test_NoDataRace_ReferenceType_Static_CorrectLocks), "Eraser")]
        [InlineData(nameof(Program.Test_NoDataRace_ReferenceType_Static_CorrectLocks), "FastTrack")]
        [InlineData(nameof(Program.Test_NoDataRace_ReferenceType_Instance_CorrectLocks), "Eraser")]
        [InlineData(nameof(Program.Test_NoDataRace_ReferenceType_Instance_CorrectLocks), "FastTrack", Skip = "Unstable")]
        [InlineData(nameof(Program.Test_NoDataRace_ReferenceType_Instance_DifferentInstances), "Eraser")]
        [InlineData(nameof(Program.Test_NoDataRace_ReferenceType_Instance_DifferentInstances), "FastTrack", Skip = "Unstable")]
        [InlineData(nameof(Program.Test_NoDataRace_ValueType_Static_CorrectLocks), "Eraser")]
        [InlineData(nameof(Program.Test_NoDataRace_ValueType_Static_CorrectLocks), "FastTrack")]
        [InlineData(nameof(Program.Test_NoDataRace_ValueType_Instance_CorrectLocks), "Eraser")]
        [InlineData(nameof(Program.Test_NoDataRace_ValueType_Instance_CorrectLocks), "FastTrack", Skip = "Unstable")]
        [InlineData(nameof(Program.Test_NoDataRace_ValueType_Instance_DifferentInstances), "Eraser")]
        [InlineData(nameof(Program.Test_NoDataRace_ValueType_Instance_DifferentInstances), "FastTrack", Skip = "Unstable")]
        public async Task DataRaceDetectionTests_NoDataRace(string testName, string plugin)
        {
            // Prepare
            await using var session = SessionHelpers.CreateAnalysisSession(TestsConfiguration.SubjectDllPath, plugin, testName);

            // Act
            await session.Start();
            var noDataRace = true;
            var reportsReader = session.GetRequiredService<IReportsReaderProvider>().GetReportsReader();
            await foreach (var report in reportsReader.ReadAllAsync())
            {
                if (report.Category == EraserPlugin.DiagnosticsCategory)
                {
                    noDataRace = false;
                    break;
                }
            }

            // Assert
            Assert.True(noDataRace);
        }
    }
}