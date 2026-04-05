// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Worker.Configuration;

public static class EnvironmentUtils
{
    public const string InstallationRootEnvVariable = "SHARPDETECT_ROOT";
    
    public static string GetSharpDetectRoot()
    {
        return FindSharpDetectFolder().Replace(Path.DirectorySeparatorChar, '/');
        
        static string FindSharpDetectFolder()
        {
            var assemblyDir = Path.GetFullPath(Path.GetDirectoryName(typeof(EnvironmentUtils).Assembly.Location)!);

            var candidate = assemblyDir;
            while (candidate is not null)
            {
                // Deployed layout
                if (Directory.Exists(Path.Combine(candidate, "Profilers")))
                    return candidate;

                // Local-build layout
                var artifacts = Path.Combine(candidate, "artifacts");
                if (Directory.Exists(Path.Combine(artifacts, "Profilers")))
                    return artifacts;

                candidate = Path.GetDirectoryName(candidate);
            }

            // Fallback (previous behavior)
            return assemblyDir;
        }
    }
}