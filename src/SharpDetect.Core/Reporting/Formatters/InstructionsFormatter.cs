// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Reporting.Formatters;

public static class InstructionsFormatter
{
    public static string FormatIlOffset(uint offset)
        => $"IL_{offset:X4}";
}
