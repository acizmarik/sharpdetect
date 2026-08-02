// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Core.Reporting.Formatters;

public static class TokenFormatters
{
    public static string FormatMethodToken(MdMethodDef token)
        => $"0x{token.Value:X8}";
}
