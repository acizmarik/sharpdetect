﻿// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common.Runtime.Threads;
using SharpDetect.Common.SourceLinks;

namespace SharpDetect.Common.Diagnostics
{
    public abstract record ReportBase(
        string Reporter, 
        string Category, 
        string MessageFormat, 
        object?[]? Arguments,
        ReportDataEntry[]? Entries);
}
