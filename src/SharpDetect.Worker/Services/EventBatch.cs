// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Worker.Services;

internal readonly record struct EventBatch(RecordedEvent[] Buffer, int Count);
