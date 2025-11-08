// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using SharpDetect.Core.Events;

namespace SharpDetect.Core.Communication;

public interface IProfilerEventReceiver
{
    bool TryReceiveNotification([NotNullWhen(true)] out RecordedEvent? recordedEvent);
}