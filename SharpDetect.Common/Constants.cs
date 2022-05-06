namespace SharpDetect.Common
{
    public static class Constants
    {
        public static class Environment
        {
            public const string SharpDetectPluginsRootFolder = "SHARPDETECT_PLUGINS";
        }

        public static class Serilog
        {
            public const string Level = "Serilog:MinimumLevel";
            public const string Template = "Serilog:Template";
        }

        public static class Configuration
        {
            public const string TargetAssembly = "TargetAssembly";
            public const string PluginsChain = "PluginsChain";
        }

        public static class Communication
        {
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
    }
}