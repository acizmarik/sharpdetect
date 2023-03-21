// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliWrap;
using System.Text;

namespace SharpDetect.TestUtils.E2E
{
    public static class CompilationHelpers
    {
        public static async Task CompileTestAsync(string path)
        {
            var sb = new StringBuilder();

            try
            {
                await Cli.Wrap("dotnet")
                    .WithArguments($"build")
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
