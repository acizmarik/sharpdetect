using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace SharpDetect.Instrumentation.Stubs
{
    public class ResolvedMethodStubs : Dictionary<Instruction, MDToken>
    {
    }
}
