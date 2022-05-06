using dnlib.DotNet;

namespace SharpDetect.Common
{
    public record struct FunctionInfo(UIntPtr ModuleId, MDToken TypeToken, MDToken FunctionToken);
}
