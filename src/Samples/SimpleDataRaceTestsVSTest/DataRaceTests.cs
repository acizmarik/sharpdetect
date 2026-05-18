// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Shared;
using Xunit;

namespace SimpleDataRaceTestsVSTest;

public class DataRaceTests
{
    [Fact]
    public void RaceFact() => DataRaceWorkloads.RaceFact();

    [Fact]
    public void CleanFact() => DataRaceWorkloads.CleanFact();
}
