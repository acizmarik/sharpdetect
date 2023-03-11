// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common.Diagnostics;

namespace SharpDetect.Common.Services.Reporting
{
    public interface IReportsRenderer
    {
        void Render(IEnumerable<ReportBase> reports);
    }
}
