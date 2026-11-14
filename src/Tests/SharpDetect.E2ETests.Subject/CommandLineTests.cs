// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.E2ETests.Subject
{
    public static partial class Program
    {
        public static void Test_EchoArguments(string[] args)
        {
            File.WriteAllLines(args[1], args.Skip(2));
        }
    }
}
