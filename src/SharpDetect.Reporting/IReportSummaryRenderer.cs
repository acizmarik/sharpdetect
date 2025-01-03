// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using SharpDetect.Reporting.Model;

namespace SharpDetect.Reporting;

public interface IReportSummaryRenderer
{
    string Render(Summary summary);
}
