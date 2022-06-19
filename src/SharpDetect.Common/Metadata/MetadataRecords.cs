using dnlib.DotNet;

namespace SharpDetect.Common.Metadata
{
    public record struct HelperMethodRefMDToken(MDToken Token);
    public record struct WrapperMethodRefMDToken(MDToken Token);
    public record struct HelperMethodDef(MethodDef Method);
    public record struct ExternMethodDef(MethodDef Method);
    public record struct WrapperMethodDef(MethodDef Method);
}
