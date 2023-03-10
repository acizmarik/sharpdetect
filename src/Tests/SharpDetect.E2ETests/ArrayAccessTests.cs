using SharpDetect.Common.Plugins;
using SharpDetect.Common.Services.Reporting;
using SharpDetect.E2ETests.Definitions;
using SharpDetect.E2ETests.Subject;
using SharpDetect.E2ETests.Utilities;
using Xunit;

namespace SharpDetect.E2ETests
{
    [Collection("E2E")]
    public class ArrayAccessTests
    {
        [Theory]
        [InlineData(nameof(Program.Test_Array_I_Read), false)]
        [InlineData(nameof(Program.Test_Array_I1_Read), false)]
        [InlineData(nameof(Program.Test_Array_I2_Read), false)]
        [InlineData(nameof(Program.Test_Array_I4_Read), false)]
        [InlineData(nameof(Program.Test_Array_I8_Read), false)]
        [InlineData(nameof(Program.Test_Array_U1_Read), false)]
        [InlineData(nameof(Program.Test_Array_U2_Read), false)]
        [InlineData(nameof(Program.Test_Array_U4_Read), false)]
        [InlineData(nameof(Program.Test_Array_U8_Read), false)]
        [InlineData(nameof(Program.Test_Array_R4_Read), false)]
        [InlineData(nameof(Program.Test_Array_R8_Read), false)]
        [InlineData(nameof(Program.Test_Array_Ref_Read), false)]
        [InlineData(nameof(Program.Test_Array_Struct_Read), false)]
        [InlineData(nameof(Program.Test_Array_I_Write), true)]
        [InlineData(nameof(Program.Test_Array_I1_Write), true)]
        [InlineData(nameof(Program.Test_Array_I2_Write), true)]
        [InlineData(nameof(Program.Test_Array_I4_Write), true)]
        [InlineData(nameof(Program.Test_Array_I8_Write), true)]
        [InlineData(nameof(Program.Test_Array_U1_Write), true)]
        [InlineData(nameof(Program.Test_Array_U2_Write), true)]
        [InlineData(nameof(Program.Test_Array_U4_Write), true)]
        [InlineData(nameof(Program.Test_Array_U8_Write), true)]
        [InlineData(nameof(Program.Test_Array_R4_Write), true)]
        [InlineData(nameof(Program.Test_Array_R8_Write), true)]
        [InlineData(nameof(Program.Test_Array_Ref_Write), true)]
        [InlineData(nameof(Program.Test_Array_Struct_Write), true)]
        public async Task ArrayAccessTests_ReadWrite(string testName, bool isWrite)
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
            var arrayElementAccess = false;
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
                else if ((!isWrite && report.Category == nameof(IPlugin.ArrayElementRead)) || (isWrite && report.Category == nameof(IPlugin.ArrayElementWritten)))
                {
                    if (reachedTestMethod && !leftTestMethod)
                        arrayElementAccess = true;
                }
            }

            // Assert
            Assert.False(invalidProgramException);
            Assert.True(analysisStarted);
            Assert.True(reachedEntryPoint);
            Assert.True(reachedTestMethod);
            Assert.True(arrayElementAccess);
            Assert.True(leftTestMethod);
            Assert.True(leftEntryPoint);
            Assert.True(analysisEnded);
        }
    }
}
