using dnlib.DotNet;

namespace SharpDetect.Common
{
    public record struct TypeInfo(UIntPtr ModuleId, MDToken TypeToken);
}
