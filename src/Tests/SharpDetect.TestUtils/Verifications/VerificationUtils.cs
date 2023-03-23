// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliWrap;
using SharpDetect.Common;
using System.Text;
using Xunit;

namespace SharpDetect.TestUtils.Verifications
{
    public static class VerificationUtils
    {
        public static IEnumerable<KeyValuePair<string, string>> GetConfigurationAdditions()
        {
            return new[]
            {
                new KeyValuePair<string, string>(Constants.Verification.Enabled, "True"),
                new KeyValuePair<string, string>(Constants.Verification.AssembliesOutputFolder, E2ETestsConfiguration.ILVerificationPath)
            };
        }

        public static async Task AssertAllMethodsAndTypesVerified(string analysisTarget, string publishDirectory)
        {
            var stdoutSb = new StringBuilder();
            var verificationResult = await Cli.Wrap("ilverify")
                .WithArguments($"{analysisTarget} -r *.dll")
                .WithValidation(CommandResultValidation.None)
                .WithWorkingDirectory(publishDirectory)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdoutSb))
                .ExecuteAsync();

            // Assert
            var path = Path.Combine(publishDirectory, analysisTarget);
            Assert.Equal(0, verificationResult.ExitCode);
            Assert.Equal($"All Classes and Methods in {path} Verified.{Environment.NewLine}", stdoutSb.ToString());
        }
    }
}
