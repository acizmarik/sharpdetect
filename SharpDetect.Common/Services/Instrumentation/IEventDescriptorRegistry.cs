using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using SharpDetect.Common.SourceLinks;

namespace SharpDetect.Common.Services.Instrumentation
{
    public interface IEventDescriptorRegistry
    {
        SourceLink Get(ulong eventId);
        SourceLink Create(AnalysisEventType type, MethodDef method, uint instructionOffset, SequencePoint? sequencePoint = null);
    }
}
