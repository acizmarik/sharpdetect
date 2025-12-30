// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using MessagePack;

namespace SharpDetect.Core.Events.Profiler;

[MessagePackObject]
public readonly record struct ThreadId([property: Key(0)] nuint Value);

[MessagePackObject]
public readonly record struct ObjectId([property: Key(0)] nuint Value);

[MessagePackObject]
public readonly record struct ModuleId([property: Key(0)] nuint Value);

[MessagePackObject]
public readonly record struct FunctionId([property: Key(0)] nuint Value);

[MessagePackObject]
public readonly record struct AssemblyId([property: Key(0)] nuint Value);

[MessagePackObject]
public readonly record struct TrackedObjectId([property: Key(0)] nuint Value);
