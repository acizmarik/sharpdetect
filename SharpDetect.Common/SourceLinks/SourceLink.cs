using dnlib.DotNet;
using dnlib.DotNet.Pdb;

namespace SharpDetect.Common.SourceLinks
{
    public record SourceLink(ulong Id, AnalysisEventType Type, MethodDef Method, uint InstructionOffset, SequencePoint? SequencePoint = null);
}
