using dnlib.DotNet;

namespace SharpDetect.Plugins.Utilities
{
    public static class FieldDefExtensions
    {
        public static bool ShouldAnalyzeForDataRaces(this FieldDef fieldDef, TypeDef threadStaticAttribute)
        {
            // Readonly fields can not be involved in a data-race
            if (fieldDef.IsInitOnly)
                return false;

            // ThreadStatic annotated fields can not be involved in a data-race
            if (fieldDef.HasCustomAttributes && fieldDef.CustomAttributes.FirstOrDefault(a => a.AttributeType == threadStaticAttribute) != null)
                return false;

            return true;
        }
    }
}
