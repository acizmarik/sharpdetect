namespace SharpDetect.Common
{
    public static class Constants
    {        
        public static class ModuleDescriptors
        {
            public const string CoreModulesPaths = "ModuleDescriptors:Core";
            public const string ExtensionModulePaths = "ModuleDescriptors:Extensions";
        }

        public static class Serilog
        {
            public const string Level = "Serilog:MinimumLevel";
            public const string Template = "Serilog:Template";
        }

        public static class Configuration
        {
            public const string TargetAssembly = "TargetAssembly";
            public const string CommandLineArgs = "CommandLineArguments";
            public const string WorkingDirectory = "WorkingDirectory";
            public const string PluginsRootFolder = "PluginsPath";
            public const string PluginsChain = "PluginsChain";
            public const string ProfilerPath = "ProfilerPath";
        }

        public static class Communication
        {
            public static class Signals
            {
                public const string Port = "Communication:Signals:Port";
                public const string Address = "Communication:Signals:Address";
                public const string Protocol = "Communication:Signals:Protocol";
            }

            public static class Notifications
            {
                public const string Port = "Communication:Notifications:Port";
                public const string Address = "Communication:Notifications:Address";
                public const string Protocol = "Communication:Notifications:Protocol";
            }

            public static class Requests
            {
                public static class Inbound
                {
                    public const string Port = "Communication:Requests:Inbound:Port";
                    public const string Address = "Communication:Requests:Inbound:Address";
                    public const string Protocol = "Communication:Requests:Inbound:Protocol";
                }

                public static class Outbound
                {
                    public const string Port = "Communication:Requests:Outbound:Port";
                    public const string Address = "Communication:Requests:Outbound:Address";
                    public const string Protocol = "Communication:Requests:Outbound:Protocol";
                }
            }
        }

        public static class Rewriting
        {
            public const string Enabled = "Rewriting:Enabled";
            public const string Strategy = "Rewriting:Strategy";
            public const string Patterns = "Rewriting:Patterns";
        }

        public static class EntryExitHooks
        {
            public const string Enabled = "EntryExitHooks:Enabled";
            public const string Strategy = "EntryExitHooks:Strategy";
            public const string Patterns = "EntryExitHooks:Patterns";
        }

        public static class Profiling
        {
            public const string Monitor = "Profiling:Monitor";
            public const string Enable = "Profiling:Enable";
            public const string Disable = "Profiling:Disable";
        }

        public static class Tests
        {
            public const string Port = "Tests:Port";
            public const string Address = "Tests.Address";
            public const string Protocol = "Tests.Protocol";
        }
    }
}