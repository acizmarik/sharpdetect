// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Common
{
    public static class Constants
    {
        public static class TargetAssemblyIO
        {
            public static class Stdin
            {
                public static readonly string Redirect = string.Join(':', nameof(TargetAssemblyIO), nameof(Stdin), nameof(Redirect));
                public static readonly string File = string.Join(':', nameof(TargetAssemblyIO), nameof(Stdin), nameof(File));
            }

            public static class Stdout
            {
                public static readonly string Redirect = string.Join(':', nameof(TargetAssemblyIO), nameof(Stdout), nameof(Redirect));
                public static readonly string File = string.Join(':', nameof(TargetAssemblyIO), nameof(Stdout), nameof(File));
            }

            public static class Stderr
            {
                public static readonly string Redirect = string.Join(':', nameof(TargetAssemblyIO), nameof(Stderr), nameof(Redirect));
                public static readonly string File = string.Join(':', nameof(TargetAssemblyIO), nameof(Stderr), nameof(File));
            }
        }

        public static class ModuleDescriptors
        {
            public static readonly string Core = string.Join(':', nameof(ModuleDescriptors), nameof(Core));
            public static readonly string Extensions = string.Join(':', nameof(ModuleDescriptors), nameof(Extensions));
        }

        public static class Serilog
        {
            public static readonly string MinimumLevel = string.Join(':', nameof(Serilog), nameof(MinimumLevel));
            public static readonly string Template = string.Join(':', nameof(Serilog), nameof(Template));
        }

        public static class Configuration
        {
            public static readonly string TargetAssembly = nameof(TargetAssembly);
            public static readonly string CommandLineArgs = nameof(CommandLineArgs);
            public static readonly string WorkingDirectory = nameof(WorkingDirectory);
            public static readonly string PluginsPath = nameof(PluginsPath);
            public static readonly string PluginsChain = nameof(PluginsChain);
            public static readonly string ProfilerPath = nameof(ProfilerPath);
            public static readonly string Plugins = nameof(Plugins);
        }

        public static class Communication
        {
            public static class Signals
            {
                public static readonly string Port = string.Join(':', nameof(Communication), nameof(Signals), nameof(Port));
                public static readonly string Address = string.Join(':', nameof(Communication), nameof(Signals), nameof(Address));
                public static readonly string Protocol = string.Join(':', nameof(Communication), nameof(Signals), nameof(Protocol));
            }

            public static class Notifications
            {
                public static readonly string Port = string.Join(':', nameof(Communication), nameof(Notifications), nameof(Port));
                public static readonly string Address = string.Join(':', nameof(Communication), nameof(Notifications), nameof(Address));
                public static readonly string Protocol = string.Join(':', nameof(Communication), nameof(Notifications), nameof(Protocol));
            }

            public static class Requests
            {
                public static class Inbound
                {
                    public static readonly string Port = string.Join(':', nameof(Communication), nameof(Requests), nameof(Inbound), nameof(Port));
                    public static readonly string Address = string.Join(':', nameof(Communication), nameof(Requests), nameof(Inbound), nameof(Address));
                    public static readonly string Protocol = string.Join(':', nameof(Communication), nameof(Requests), nameof(Inbound), nameof(Protocol));
                }

                public static class Outbound
                {
                    public static readonly string Port = string.Join(':', nameof(Communication), nameof(Requests), nameof(Outbound), nameof(Port));
                    public static readonly string Address = string.Join(':', nameof(Communication), nameof(Requests), nameof(Outbound), nameof(Address));
                    public static readonly string Protocol = string.Join(':', nameof(Communication), nameof(Requests), nameof(Outbound), nameof(Protocol));
                }
            }
        }

        public static class Rewriting
        {
            public static readonly string Enabled = string.Join(':', nameof(Rewriting), nameof(Enabled));
            public static readonly string Strategy = string.Join(':', nameof(Rewriting), nameof(Strategy));
            public static readonly string Patterns = string.Join(':', nameof(Rewriting), nameof(Patterns));
        }

        public static class EntryExitHooks
        {
            public static readonly string Enabled = string.Join(':', nameof(EntryExitHooks), nameof(Enabled));
            public static readonly string Strategy = string.Join(':', nameof(EntryExitHooks), nameof(Strategy));
            public static readonly string Patterns = string.Join(':', nameof(EntryExitHooks), nameof(Patterns));
        }

        public static class Profiling
        {
            public static readonly string Monitor = string.Join(':', nameof(Profiling), nameof(Monitor));
            public static readonly string Enable = string.Join(':', nameof(Profiling), nameof(Enable));
            public static readonly string Disable = string.Join(':', nameof(Profiling), nameof(Disable));
        }

        public static class Tests
        {
            public static readonly string Port = string.Join(':', nameof(Tests), nameof(Port));
            public static readonly string Address = string.Join(':', nameof(Tests), nameof(Address));
            public static readonly string Protocol = string.Join(':', nameof(Tests), nameof(Protocol));
        }
    }
}