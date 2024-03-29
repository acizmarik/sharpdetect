﻿// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Google.Protobuf;
using SharpDetect.Common;
using SharpDetect.Common.LibraryDescriptors;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Services.Endpoints;
using System.Runtime.InteropServices;

namespace SharpDetect.Core.Communication.Endpoints
{
    public class ProfilingClient : IProfilingClient
    {
        private readonly IRequestsProducer requestsProducer;
        private ulong requestCounter;

        public ProfilingClient(IRequestsProducer requestsProducer)
        {
            this.requestsProducer = requestsProducer;
        }

        /// <summary>
        /// Send request to process with processId to perform no changes based on the provided info
        /// </summary>
        /// <param name="processId">Process ID to send to request to</param>
        /// <param name="info">Event we are responding to</param>
        /// <returns>Request response</returns>
        public Task<Response> IssueNoChangesRequestAsync(RawEventInfo info)
        {
            var request = new RequestMessage()
            {
                NotificationId = info.Id,
                RequestId = Interlocked.Increment(ref requestCounter),
                NoChanges = new Request_NoChanges()
            };

            return requestsProducer.SendAsync(info.ProcessId, request);
        }

        /// <summary>
        /// Send request to continue with execution based on the provided info
        /// </summary>
        /// <param name="info">Event we are responding to</param>
        /// <returns>Request response</returns>
        public Task<Response> IssueContinueExecutionRequestAsync(RawEventInfo info)
        {
            var request = new RequestMessage()
            {
                NotificationId = info.Id,
                RequestId = Interlocked.Increment(ref requestCounter),
                ContinueExecution = new Request_ContinueExecution()
            };

            return requestsProducer.SendAsync(info.ProcessId, request);
        }

        /// <summary>
        /// Send request to terminate execution
        /// </summary>
        /// <param name="info">Event we are responding to</param>
        /// <returns>Request response</returns>
        public Task<Response> IssueTerminationRequestAsync(RawEventInfo info)
        {
            var request = new RequestMessage()
            {
                NotificationId = info.Id,
                RequestId = Interlocked.Increment(ref requestCounter),
                Termination = new Request_Termination()
            };

            return requestsProducer.SendAsync(info.ProcessId, request);
        }

        /// <summary>
        /// Send request to emit method wrappers for provided method infos
        /// </summary>
        /// <param name="methods">Method wrappers information</param>
        /// <param name="info">Event we are responding to</param>
        public Task<Response> IssueEmitMethodWrappersRequestAsync(IEnumerable<(FunctionInfo Function, ushort Argc)> methods, RawEventInfo info)
        {
            var request = new RequestMessage()
            {
                NotificationId = info.Id,
                RequestId = Interlocked.Increment(ref requestCounter),
                Wrapping = new Request_Wrapping()
            };

            foreach (var (function, argc) in methods)
            {
                request.Wrapping.MethodsToWrap.Add(new ExternalMethodInfo()
                {
                    TypeToken = function.TypeToken.Raw,
                    FunctionToken = function.FunctionToken.Raw,
                    ParametersCount = argc
                });
            }

            return requestsProducer.SendAsync(info.ProcessId, request);
        }

        /// <summary>
        /// Send request to rewrite IL body and/or add hooks for the method that is about to be JIT compiled based on the provided event info
        /// </summary>
        /// <param name="bytecode">New bytecode</param>
        /// <param name="methodData">Method information</param>
        /// <param name="info">Event we are responding to</param>
        /// <returns></returns>
        public Task<Response> IssueRewriteMethodBodyAsync(byte[]? bytecode, MethodInterpretationData? methodData, bool overrideIssueHooks, RawEventInfo info)
        {
            var request = new RequestMessage
            {
                RequestId = Interlocked.Increment(ref requestCounter),
                NotificationId = info.Id,

                Instrumentation = new Request_Instrumentation()
                {
                    InjectHooks = (overrideIssueHooks || methodData?.Flags.HasFlag(MethodRewritingFlags.InjectEntryExitHooks) == true),
                    CaptureArguments = (methodData?.Flags.HasFlag(MethodRewritingFlags.CaptureArguments) == true),
                    CaptureReturnValue = (methodData?.Flags.HasFlag(MethodRewritingFlags.CaptureReturnValue) == true)
                }
            };

            // Set new bytecode if available
            if (bytecode != null)
                request.Instrumentation.Bytecode = ByteString.CopyFrom(bytecode);

            // Set parameters information if available
            if (methodData?.CapturedParams != null && methodData.CapturedParams.Length > 0)
            {
                var paramsInfo = methodData.CapturedParams;
                var argumentInfos = new ushort[paramsInfo.Length * 2];
                var indirectInfos = new byte[(int)Math.Ceiling(paramsInfo.Length / 8f)];

                var arrayPointer = 0;
                for (var i = 0; i < paramsInfo.Length; i++)
                {
                    var (index, size, indirectLoad) = paramsInfo[i];

                    // Argument index
                    argumentInfos[arrayPointer] = index;
                    // Argument size
                    argumentInfos[arrayPointer + 1] = size;
                    // Indirect load
                    var isIndirect = indirectLoad ? 1 : 0;
                    indirectInfos[i / 8] = (byte)(isIndirect << (i % 8));
                    arrayPointer += 2;
                }

                var byteArray = MemoryMarshal.Cast<ushort, byte>(argumentInfos);
                request.Instrumentation.ArgumentInfos = ByteString.CopyFrom(byteArray);
                request.Instrumentation.PassingByRefInfos = ByteString.CopyFrom(indirectInfos);
            }

            return requestsProducer.SendAsync(info.ProcessId, request);
        }
    }
}