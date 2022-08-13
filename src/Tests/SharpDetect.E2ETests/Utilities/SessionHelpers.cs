using SharpDetect.Common;
using SharpDetect.Common.Instrumentation;

namespace SharpDetect.E2ETests.Utilities
{
    public static class SessionHelpers
    {
        private static readonly string ProfilerName;
        private static readonly string ProfilerDllPath;

        static SessionHelpers()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                ProfilerName = "SharpDetect.Profiler.dll";
            else if (Environment.OSVersion.Platform == PlatformID.Unix)
                ProfilerName = "libSharpDetect.Profiler.so";
            else
                throw new PlatformNotSupportedException();
            ProfilerDllPath = Path.Combine("..", "..", "..", "..", "..", "SharpDetect.Profiler", "build", "bin", ProfilerName);
        }

        public static AnalysisSession CreateAnalysisSession(string executablePath, string plugins)
        {
            return new AnalysisSession(executablePath, new[]
            {
                new KeyValuePair<string, string>(Constants.Configuration.PluginsChain, plugins),
                new KeyValuePair<string, string>(Constants.Configuration.PluginsRootFolder, Directory.GetCurrentDirectory()),
                new KeyValuePair<string, string>(Constants.Configuration.ProfilerPath, ProfilerDllPath),
                new KeyValuePair<string, string>(Constants.Instrumentation.Enabled, "True"),
                new KeyValuePair<string, string>(Constants.Instrumentation.Strategy, nameof(InstrumentationStrategy.OnlyPatterns)),
                new KeyValuePair<string, string>($"{Constants.Instrumentation.Patterns}:0", TestsConfiguration.SubjectNamespace)
            });
        }
    }
}
