using SharpDetect.Common.Messages;

namespace SharpDetect.Common.Services.Endpoints
{
    public interface IRequestsProducer : IEndpoint
    {
        Task<Response> SendAsync(int processId, RequestMessage message);
    }
}
