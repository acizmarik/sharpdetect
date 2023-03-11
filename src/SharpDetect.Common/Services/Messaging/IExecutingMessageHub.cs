// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Google.Protobuf;

namespace SharpDetect.Common.Services
{
    public record struct RawArgumentsList(ByteString ArgValues, ByteString ArgOffsets);
    public record struct RawReturnValue(ByteString ReturnValue);

    public interface IExecutingMessageHub : INotificationsHandler
    {
        event Action<(FunctionInfo Function, RawArgumentsList? Arguments, RawEventInfo Info)> MethodCalled;
        event Action<(FunctionInfo Function, RawReturnValue? ReturnValue, RawArgumentsList? ByRefArguments, RawEventInfo Info)> MethodReturned;
    }
}
