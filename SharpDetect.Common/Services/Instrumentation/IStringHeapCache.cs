using dnlib.DotNet;

namespace SharpDetect.Common.Services.Instrumentation
{
    public interface IStringHeapCache
    {
        MDToken GetStringOffset(ModuleDef module, string str);
    }
}
