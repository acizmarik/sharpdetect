// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliWrap;
using System.Text;

namespace SharpDetect.TestUtils.Verifications
{
    public static class PublishHelpers
    {

        public static async Task PublishTestAsync(string path, string outputDirectory, bool selfContained = true)
        {
            var sb = new StringBuilder();

            try
            {
                await Cli.Wrap("dotnet")
                    .WithArguments($"publish -o {outputDirectory} {(selfContained ? "--self-contained" : string.Empty)}")
                    .WithValidation(CommandResultValidation.ZeroExitCode)
                    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(sb))
                    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(sb))
                    .WithWorkingDirectory(path)
                    .ExecuteAsync();
            }
            catch
            {
                throw new ArgumentException("Could not compile due to the following error: " + sb.ToString());
            }
        }
    }
}
