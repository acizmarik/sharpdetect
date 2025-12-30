// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using MessagePack;

namespace SharpDetect.Core.Events.Profiler;

[MessagePackObject]
public readonly record struct MdToken([property: Key(0)] int Value)
{
    public const uint RID_MASK = 0xFF000000;

    public MdTokenType GetTokenType()
        => (MdTokenType)(Value & RID_MASK);
}

[MessagePackObject]
public readonly record struct MdTypeDef([property: Key(0)] int Value);

[MessagePackObject]
public readonly record struct MdTypeRef([property: Key(0)] int Value);

[MessagePackObject]
public readonly record struct MdMethodDef([property: Key(0)] int Value);

[MessagePackObject]
public readonly record struct MdMemberRef([property: Key(0)] int Value);