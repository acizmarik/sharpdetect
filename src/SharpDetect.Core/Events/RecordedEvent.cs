// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Events;

public sealed record RecordedEvent(
    RecordedEventMetadata Metadata,
    IRecordedEventArgs EventArgs);
