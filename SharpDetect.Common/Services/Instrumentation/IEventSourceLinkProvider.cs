using SharpDetect.Common.SourceLinks;

namespace SharpDetect.Common.Services.Instrumentation
{
    public interface IEventSourceLinkProvider
    {
        SourceLink GetSourceLink(ulong eventId);
        bool TryGetSourceLink(ulong eventId, out SourceLink sourceLink);
    }
}
