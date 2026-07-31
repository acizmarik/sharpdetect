// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Core.Communication;

public interface IProfilerEventReceiver
{
    int TryReceiveNotifications(Span<RecordedEvent> destination, out int failedRecordsCount);
}
