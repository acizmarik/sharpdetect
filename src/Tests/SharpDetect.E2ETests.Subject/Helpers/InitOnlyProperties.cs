// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.E2ETests.Subject.Helpers;

// An init accessor returns modreq(IsExternalInit) void
public class InitOnlyReferenceType
{
    public int Value { get; init; }
}

public struct InitOnlyValueType
{
    public int Value { get; init; }
}