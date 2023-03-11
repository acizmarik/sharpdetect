// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common.Diagnostics;
using System.Threading.Channels;

namespace SharpDetect.Common.Services.Reporting
{
    public interface IReportsReaderProvider
    {
        ChannelReader<ReportBase> GetReportsReader();
    }
}
