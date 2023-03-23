// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.TestUtils
{
    public static class E2ETestsConfiguration
    {
        public const string SubjectNamespace = "SharpDetect.E2ETests.Subject";
        public static readonly string SubjectCsprojPath;
        public static readonly string ILVerificationPath;
        public static readonly string SubjectDllPath;

        static E2ETestsConfiguration()
        {
#if DEBUG
            var buildType = "Debug";
#elif RELEASE        
            var buildType = "Release";
#endif
            SubjectCsprojPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../../..", SubjectNamespace));
            SubjectDllPath = Path.GetFullPath(Path.Combine(SubjectCsprojPath, $"bin/{buildType}/net7.0/", $"{SubjectNamespace}.dll"));
            ILVerificationPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(SubjectDllPath)!, "_ilverify"));
        }
    }
}
