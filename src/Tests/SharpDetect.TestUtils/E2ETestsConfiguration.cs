// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.TestUtils
{
    public static class E2ETestsConfiguration
    {
        public const string SubjectNamespace = "SharpDetect.E2ETests.Subject";
        public const string SubjectCsprojPath = "../../../../" + SubjectNamespace;
        public static readonly string SubjectDllPath;

        static E2ETestsConfiguration()
        {
#if DEBUG
            var buildType = "Debug";
#elif RELEASE        
            var buildType = "Release";
#endif
            SubjectDllPath = SubjectCsprojPath + $"/bin/{buildType}/net7.0/" + $"{SubjectNamespace}.dll";
        }
    }
}
