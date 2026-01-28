// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;
using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Core.Reporting;

public record SummaryRenderingContext(
    Summary Summary,
    IPlugin Plugin,
    DirectoryInfo AdditionalPartials);