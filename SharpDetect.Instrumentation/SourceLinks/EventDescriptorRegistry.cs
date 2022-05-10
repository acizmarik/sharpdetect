using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using SharpDetect.Common;
using SharpDetect.Common.Services.Instrumentation;
using SharpDetect.Common.SourceLinks;

namespace SharpDetect.Instrumentation.SourceLinks
{
    internal class EventDescriptorRegistry : IEventDescriptorRegistry
    {
        public SourceLink Create(AnalysisEventType type, MethodDef method, uint instructionOffset, SequencePoint? sequencePoint = null)
        {
            throw new NotImplementedException();
        }

        public SourceLink Get(ulong eventId)
        {
            throw new NotImplementedException();
        }
    }
}
