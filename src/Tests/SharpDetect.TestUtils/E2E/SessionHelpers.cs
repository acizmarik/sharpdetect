﻿// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common;
using SharpDetect.Common.Instrumentation;
using System.Runtime.CompilerServices;

namespace SharpDetect.TestUtils.E2E
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
                ProfilerName = "SharpDetect.Profiler.so";
                ProfilerDllPath = Path.Combine(pathPrefix, "linux-x64", "publish", ProfilerName);
            }
            else
            {
                // Apple..
                throw new PlatformNotSupportedException();
            }
        }

        public static AnalysisSession CreateAnalysisSession(
            string executablePath, 
            string plugins,
            string? args = null,
            IEnumerable<KeyValuePair<string, string>>? additionalConfiguration = null,
            [CallerFilePath] string? filePath = null, 
            [CallerMemberName] string? callerMemberName = null)
        {
            var fileNameSuffix = $"{Path.GetFileNameWithoutExtension(filePath)}-{callerMemberName}";
            return new AnalysisSession(executablePath, new[]
            {
                new KeyValuePair<string, string>(Constants.Configuration.PluginsChain, plugins),
                new KeyValuePair<string, string>(Constants.Configuration.PluginsPath, Directory.GetCurrentDirectory()),
                new KeyValuePair<string, string>(Constants.Configuration.ProfilerPath, ProfilerDllPath),
                new KeyValuePair<string, string>(Constants.Configuration.CommandLineArgs, args ?? string.Empty),

                // Rewriting options
                new KeyValuePair<string, string>(Constants.Rewriting.Enabled, "True"),
                new KeyValuePair<string, string>(Constants.Rewriting.Strategy, nameof(InstrumentationStrategy.OnlyPatterns)),
                new KeyValuePair<string, string>($"{Constants.Rewriting.Patterns}:0:Pattern", E2ETestsConfiguration.SubjectNamespace),
                new KeyValuePair<string, string>($"{Constants.Rewriting.Patterns}:0:Target", nameof(InstrumentationTarget.Method)),
                new KeyValuePair<string, string>($"{Constants.Rewriting.Patterns}:1:Pattern", E2ETestsConfiguration.SubjectNamespace),
                new KeyValuePair<string, string>($"{Constants.Rewriting.Patterns}:1:Target", nameof(InstrumentationTarget.Field)),

                // Hook options
                new KeyValuePair<string, string>(Constants.EntryExitHooks.Enabled, "True"),
                new KeyValuePair<string, string>(Constants.EntryExitHooks.Strategy, nameof(InstrumentationStrategy.OnlyPatterns)),
                new KeyValuePair<string, string>($"{Constants.EntryExitHooks.Patterns}:0", E2ETestsConfiguration.SubjectNamespace),

                // Stdout, Stderr redirections
                new KeyValuePair<string, string>(Constants.TargetAssemblyIO.Stdout.Redirect, "True"),
                new KeyValuePair<string, string>(Constants.TargetAssemblyIO.Stdout.File, $"stdout-{fileNameSuffix}.txt"),
                new KeyValuePair<string, string>(Constants.TargetAssemblyIO.Stderr.Redirect, "True"),
                new KeyValuePair<string, string>(Constants.TargetAssemblyIO.Stderr.File, $"stderr-{fileNameSuffix}.txt")
            }.Concat(additionalConfiguration ?? Enumerable.Empty<KeyValuePair<string, string>>()).ToArray());
        }
    }
}
