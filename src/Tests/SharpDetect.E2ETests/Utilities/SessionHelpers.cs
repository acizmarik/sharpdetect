using SharpDetect.Common;
using SharpDetect.Common.Instrumentation;
using SharpDetect.E2ETests.Definitions;

namespace SharpDetect.E2ETests.Utilities
{
    public static class SessionHelpers
    {
        private const string Configuration =
#if DEBUG
            "Debug";
#endif
#if RELEASE
            "Release";
#endif
        private static readonly string ProfilerName;
        private static readonly string ProfilerDllPath;

        static SessionHelpers()
        {
            var pathPrefix = Path.Combine("..", "..", "..", "..", "..", "SharpDetect.Profiler", "bin", Configuration, "net7.0");
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                ProfilerName = "SharpDetect.Profiler.dll";
                ProfilerDllPath = Path.Combine(pathPrefix, "win-x64", "publish", ProfilerName);
            }
            else if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                ProfilerName = "libSharpDetect.Profiler.so";
                ProfilerDllPath = Path.Combine(pathPrefix, "linux-x64", "publish", ProfilerName);
            }
            else
            {
                // Apple..
                throw new PlatformNotSupportedException();
            }
        }

        public static AnalysisSession CreateAnalysisSession(string executablePath, string plugins, string? args = null)
        {
            return new AnalysisSession(executablePath, new[]
            {
                new KeyValuePair<string, string>(Constants.Configuration.PluginsChain, plugins),
                new KeyValuePair<string, string>(Constants.Configuration.PluginsPath, Directory.GetCurrentDirectory()),
                new KeyValuePair<string, string>(Constants.Configuration.ProfilerPath, ProfilerDllPath),
                new KeyValuePair<string, string>(Constants.Configuration.CommandLineArgs, args ?? string.Empty),

                // Rewriting options
                new KeyValuePair<string, string>(Constants.Rewriting.Enabled, "True"),
                new KeyValuePair<string, string>(Constants.Rewriting.Strategy, nameof(InstrumentationStrategy.OnlyPatterns)),
                new KeyValuePair<string, string>($"{Constants.Rewriting.Patterns}:0:Pattern", TestsConfiguration.SubjectNamespace),
                new KeyValuePair<string, string>($"{Constants.Rewriting.Patterns}:0:Target", nameof(InstrumentationTarget.Method)),
                new KeyValuePair<string, string>($"{Constants.Rewriting.Patterns}:1:Pattern", TestsConfiguration.SubjectNamespace),
                new KeyValuePair<string, string>($"{Constants.Rewriting.Patterns}:1:Target", nameof(InstrumentationTarget.Field)),

                // Hook options
                new KeyValuePair<string, string>(Constants.EntryExitHooks.Enabled, "True"),
                new KeyValuePair<string, string>(Constants.EntryExitHooks.Strategy, nameof(InstrumentationStrategy.OnlyPatterns)),
                new KeyValuePair<string, string>($"{Constants.EntryExitHooks.Patterns}:0", TestsConfiguration.SubjectNamespace)
            });
        }
    }
}
