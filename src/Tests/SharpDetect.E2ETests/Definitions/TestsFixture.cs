// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.E2ETests.Utilities;

namespace SharpDetect.E2ETests.Definitions
{
    public class CompilationFixture
    {
        public CompilationFixture()
        {
            Task.Run(() => CompilationHelpers.CompileTestAsync(TestsConfiguration.SubjectCsprojPath)).Wait();
        }
    }
}
