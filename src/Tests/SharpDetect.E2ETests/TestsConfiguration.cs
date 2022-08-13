﻿namespace SharpDetect.E2ETests
{
    internal static class TestsConfiguration
    {
        public const string SubjectNamespace = "SharpDetect.E2ETests.Subject";
        public const string SubjectCsprojPath = "../../../../" + SubjectNamespace;
        public static readonly string SubjectDllPath;

        static TestsConfiguration()
        {
#if DEBUG
            var buildType = "Debug";
#elif RELEASE        
            var buildType = "Release";
#endif
            SubjectDllPath = SubjectCsprojPath + $"/bin/{buildType}/net6.0/" + $"{SubjectNamespace}.dll";
        }
    }
}
