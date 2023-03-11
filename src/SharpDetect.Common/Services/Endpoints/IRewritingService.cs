// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common.LibraryDescriptors;
using SharpDetect.Common.Messages;

namespace SharpDetect.Common.Services.Endpoints
{
    public interface IProfilingClient
    {
        Task<Response> IssueNoChangesRequestAsync(RawEventInfo info);
        Task<Response> IssueContinueExecutionRequestAsync(RawEventInfo info);


        Task<Response> IssueEmitMethodWrappersRequestAsync(IEnumerable<(FunctionInfo Function, ushort Argc)> methods, RawEventInfo info);
        Task<Response> IssueRewriteMethodBodyAsync(byte[]? bytecode, MethodInterpretationData? methodData, bool overrideHooks, RawEventInfo info);

        Task<Response> IssueTerminationRequestAsync(RawEventInfo info);
    }
}
