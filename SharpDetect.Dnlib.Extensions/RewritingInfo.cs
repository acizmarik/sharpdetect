using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace SharpDetect.Dnlib.Extensions
{
    public sealed class RewritingInfo
    {
        public readonly IReadOnlyDictionary<Instruction, MDToken> Stubs = new Dictionary<Instruction, MDToken>();

        public void AddStubInfo(Instruction instruction, MDToken correctToken)
            => ((Dictionary<Instruction, MDToken>)Stubs).Add(instruction, correctToken);
    }
}
