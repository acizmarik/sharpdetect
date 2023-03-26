// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.TestUtils.E2E
{
    public class CompilationFixture
    {
        public CompilationFixture()
        {
            Task.Run(() => CompilationHelpers.CompileTestAsync(E2ETestsConfiguration.SubjectCsprojPath)).Wait();
        }
    }
}
