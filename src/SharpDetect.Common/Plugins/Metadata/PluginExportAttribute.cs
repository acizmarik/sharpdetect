namespace SharpDetect.Common.Plugins.Metadata
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PluginExportAttribute : Attribute
    {
        public readonly string Name;
        public readonly string? Version;

        public PluginExportAttribute(string name, string? version = default)
        {
            Name = name;
            Version = version;
        }
    }
}
