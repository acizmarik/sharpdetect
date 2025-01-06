// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using OneOf;
using SharpDetect.Events.Descriptors.Profiler;

namespace SharpDetect.Events;

public record struct RuntimeArgumentInfo(ushort Index, OneOf<object, TrackedObjectId> Value);
