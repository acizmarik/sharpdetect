// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliWrap;
using SharpDetect.Common;
using SharpDetect.TestUtils;
using SharpDetect.TestUtils.E2E;
using SharpDetect.TestUtils.Verifications;
using System.Text;
using Xunit;

namespace SharpDetect.ILVerifications
{
    [Collection("E2E")]
    public class FieldAccessRewritingTests
    {
        [Fact]
        public async Task Verify_FieldElementAccess_Instrumentation()
        {
            // Prepare
            var analysisTarget = Path.GetFileName(E2ETestsConfiguration.SubjectDllPath);
            var ilverifyWorkingDirectory = 
                Path.GetFullPath(
                    Path.Combine(
                        Path.GetDirectoryName(E2ETestsConfiguration.SubjectDllPath)!,
                        "_ilverify"));
            await PublishHelpers.PublishTestAsync(E2ETestsConfiguration.SubjectCsprojPath, ilverifyWorkingDirectory);
            await using var session = SessionHelpers.CreateAnalysisSession(E2ETestsConfiguration.SubjectDllPath, "Nop", additionalConfiguration:
                new[]
                {
                    new KeyValuePair<string, string>(Constants.Verification.Enabled, "True"),
                    new KeyValuePair<string, string>(Constants.Verification.AssembliesOutputFolder, ilverifyWorkingDirectory)
                });

            //// Act
            var stdoutSb = new StringBuilder();
            //await session.Start();
            var verificationResult = await Cli.Wrap("ILVerify")
                .WithArguments($"{analysisTarget} -r *.dll")
                .WithValidation(CommandResultValidation.None)
                .WithWorkingDirectory(ilverifyWorkingDirectory)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdoutSb))
                .ExecuteAsync();

            // Assert
            Assert.Equal($"All Classes and Methods in {Path.Combine(ilverifyWorkingDirectory, analysisTarget)} Verified.{Environment.NewLine}", stdoutSb.ToString());
        }
    }
}
