// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Events;

public record struct RuntimeArgumentInfo(ushort Index, ArgumentValue Value);
