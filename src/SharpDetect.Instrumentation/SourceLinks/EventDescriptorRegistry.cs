using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using SharpDetect.Common;
using SharpDetect.Common.Exceptions;
using SharpDetect.Common.Services.Instrumentation;
using SharpDetect.Common.SourceLinks;
using System.Collections.Concurrent;

namespace SharpDetect.Instrumentation.SourceLinks
{
    internal class EventDescriptorRegistry : IEventDescriptorRegistry
    {
        private ConcurrentDictionary<ulong, SourceLink> sourceLinks;
        private ulong idCounter;

        public EventDescriptorRegistry()
        {
            this.sourceLinks = new ConcurrentDictionary<ulong, SourceLink>();
            this.idCounter = 0;
        }

        public SourceLink Create(AnalysisEventType type, MethodDef method, uint instructionOffset, SequencePoint? sequencePoint = null)
        {
            var newId = Interlocked.Increment(ref idCounter);
            var sourceLink = new SourceLink(newId, type, method, instructionOffset, sequencePoint);
            Guard.True<ArgumentException>(sourceLinks.TryAdd(newId, sourceLink));

            return sourceLink;
        }

        public SourceLink Get(ulong eventId)
        {
            return sourceLinks[eventId];
        }
    }
}
