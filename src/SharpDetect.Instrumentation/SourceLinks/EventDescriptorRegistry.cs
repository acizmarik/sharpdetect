using CommunityToolkit.Diagnostics;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
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
        private readonly ConcurrentDictionary<ulong, SourceLink> sourceLinks;
        private ulong idCounter;

        public EventDescriptorRegistry()
        {
            this.sourceLinks = new ConcurrentDictionary<ulong, SourceLink>();
            this.idCounter = 0;
        }

        public SourceLink Create(AnalysisEventType type, MethodDef method, Instruction instruction, SequencePoint? sequencePoint = null)
        {
            var newId = Interlocked.Increment(ref idCounter);
            // At this point we need to clone the instruction
            // Otherwise the offset will get shifted after instrumentation
            var instructionClone = instruction.Clone();
            var sourceLink = new SourceLink(newId, type, method, instructionClone, sequencePoint);
            if (!sourceLinks.TryAdd(newId, sourceLink))
                ThrowHelper.ThrowArgumentException($"Could not register source link {sourceLink}");

            return sourceLink;
        }

        public SourceLink Get(ulong eventId)
        {
            return sourceLinks[eventId];
        }
    }
}
