// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;

namespace SharpDetect.Worker.Configuration;

public static class EnvironmentUtils
{
    public const string InstallationRootEnvVariable = "SHARPDETECT_ROOT";
    public const string ProfilersRootEnvVariable = "SHARPDETECT_PROFILERS";

    public static void Initialize()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(InstallationRootEnvVariable)))
        {
            var sharpDetectRoot = GetSharpDetectRoot();
            Environment.SetEnvironmentVariable(InstallationRootEnvVariable, sharpDetectRoot);
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ProfilersRootEnvVariable)))
        {
            var profilersRoot = GetSharpDetectProfilers();
            Environment.SetEnvironmentVariable(ProfilersRootEnvVariable, profilersRoot);
        }
    }
    
    internal static string GetSharpDetectRoot()
    {
        return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    }

    public static string ExpandEnvironmentVariablesForPath(string path)
    {
        return Environment.ExpandEnvironmentVariables(path).Replace(Path.DirectorySeparatorChar, '/');
    }
    
    internal static string GetSharpDetectProfilers()
    {
        return FindSharpDetectFolder().Replace(Path.DirectorySeparatorChar, '/');
        
        static string FindSharpDetectFolder()
        {
            const string artifactsDirectoryName = "artifacts";
            const string profilersDirectoryName = "Profilers";
            var assemblyDir = Path.GetFullPath(Path.GetDirectoryName(typeof(EnvironmentUtils).Assembly.Location)!);

            var candidate = assemblyDir;
            while (candidate is not null)
            {
                // Deployed layout
                var deployedCandidate = Path.Combine(candidate, profilersDirectoryName);
                if (Directory.Exists(deployedCandidate))
                    return deployedCandidate;

                // Local-build layout
                var localBuildCandidate = Path.Combine(candidate, artifactsDirectoryName, profilersDirectoryName);
                if (Directory.Exists(localBuildCandidate))
                    return localBuildCandidate;

                candidate = Path.GetDirectoryName(candidate);
            }

            // Fallback (previous behavior)
            return assemblyDir;
        }
    }
}