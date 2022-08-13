using SharpDetect.Common.LibraryDescriptors;
using SharpDetect.Common.Messages;

namespace SharpDetect.Common.Services.Endpoints
{
    public interface IProfilingClient
    {
        Task<Response> IssueNoChangesRequestAsync(EventInfo info);
        Task<Response> IssueContinueExecutionRequestAsync(EventInfo info);


        Task<Response> IssueEmitMethodWrappersRequestAsync(IEnumerable<(FunctionInfo Function, ushort Argc)> methods, EventInfo info);
        Task<Response> IssueRewriteMethodBodyAsync(byte[]? bytecode, MethodInterpretationData? methodData, bool overrideHooks, EventInfo info);
    }
}
