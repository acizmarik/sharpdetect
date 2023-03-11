// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Common.Diagnostics
{
    public record WarningReport(string Reporter, string Category, string MessageFormat, object?[]? Arguments, ReportDataEntry[]? Entries)
        : ReportBase(Reporter, Category, MessageFormat, Arguments, Entries);
}
