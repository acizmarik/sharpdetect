// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Core.Plugins;

public record struct RecordedEventHandlerType(RecordedEventType EventType, Type ArgsType);
