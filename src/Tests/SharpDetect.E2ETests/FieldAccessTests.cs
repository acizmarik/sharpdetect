﻿// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common.Plugins;
using SharpDetect.Common.Services.Reporting;
using SharpDetect.E2ETests.Subject;
using SharpDetect.TestUtils;
using SharpDetect.TestUtils.E2E;
using Xunit;

namespace SharpDetect.E2ETests
{
    [Collection("E2E")]
    public class FieldAccessTests
    {
        [Theory]
        [InlineData(nameof(Program.Test_Field_ValueType_Instance_Read), false)]
        [InlineData(nameof(Program.Test_Field_ReferenceType_Instance_Read), false)]
        [InlineData(nameof(Program.Test_Field_ValueType_Static_Read), false)]
        [InlineData(nameof(Program.Test_Field_ReferenceType_Static_Read), false)]
        [InlineData(nameof(Program.Test_Field_ValueType_Instance_Write), true)]
        [InlineData(nameof(Program.Test_Field_ReferenceType_Instance_Write), true)]
        [InlineData(nameof(Program.Test_Field_ValueType_Static_Write), true)]
        [InlineData(nameof(Program.Test_Field_ReferenceType_Static_Write), true)]
        [InlineData(nameof(Program.Test_Property_ValueType_Instance_Read), false)]
        [InlineData(nameof(Program.Test_Property_ReferenceType_Instance_Read), false)]
        [InlineData(nameof(Program.Test_Property_ValueType_Static_Read), false)]
        [InlineData(nameof(Program.Test_Property_ReferenceType_Static_Read), false)]
        [InlineData(nameof(Program.Test_Property_ValueType_Instance_Write), true)]
        [InlineData(nameof(Program.Test_Property_ReferenceType_Instance_Write), true)]
        [InlineData(nameof(Program.Test_Property_ValueType_Static_Write), true)]
        [InlineData(nameof(Program.Test_Property_ReferenceType_Static_Write), true)]
        public async Task FieldAccessTests_ReadWrite(string testName, bool isWrite)
        {
            // Prepare
            await using var session = SessionHelpers.CreateAnalysisSession(E2ETestsConfiguration.SubjectDllPath, "Reporter", testName);
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
            var fieldAccess = false;
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
                else if ((!isWrite && report.Category == nameof(IPlugin.FieldRead)) || (isWrite && report.Category == nameof(IPlugin.FieldWritten)))
                {
                    if (reachedTestMethod && !leftTestMethod && report.MessageFormat.Contains(fieldName))
                    {
                        fieldAccess = true;
                    }
                }
            }

            // Assert
            Assert.False(invalidProgramException);
            Assert.True(analysisStarted);
            Assert.True(reachedEntryPoint);
            Assert.True(reachedTestMethod);
            Assert.True(fieldAccess);
            Assert.True(leftTestMethod);
            Assert.True(leftEntryPoint);
            Assert.True(analysisEnded);
        }
    }
}
