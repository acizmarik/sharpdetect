// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;
using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.E2ETests.Utils;

public interface ITestPlugin : IPlugin
{
    IEnumerable<Report> GetSubjectReports();
}