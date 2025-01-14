// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Events.Profiler;

public readonly record struct MdTokens(int Value)
{
    public const uint RID_MASK = 0xFF000000;

    public MdTokenType GetTokenType()
        => (MdTokenType)(Value & RID_MASK);
}

public readonly record struct MdTypeDef(int Value);
public readonly record struct MdTypeRef(int Value);
public readonly record struct MdMethodDef(int Value);
public readonly record struct MdMemberRef(int Value);