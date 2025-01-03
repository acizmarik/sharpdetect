// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿namespace SharpDetect.Events.Descriptors.Profiler
{
    public readonly record struct ThreadId(nuint Value);
    public readonly record struct ObjectId(nuint Value);
    public readonly record struct ModuleId(nuint Value);
    public readonly record struct FunctionId(nuint Value);
    public readonly record struct AssemblyId(nuint Value);
    public readonly record struct TrackedObjectId(nuint Value);
}
