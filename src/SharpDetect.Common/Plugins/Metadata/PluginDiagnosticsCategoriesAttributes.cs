namespace SharpDetect.Common.Plugins.Metadata
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PluginDiagnosticsCategoriesAttribute : Attribute
    {
        public readonly string[] Categories;

        public PluginDiagnosticsCategoriesAttribute(string[] categories)
        {
            Categories = categories;
        }
    }
}
