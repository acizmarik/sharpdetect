// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Shared;
using TUnit.Core;

namespace SimpleDataRaceTestsMtp;

public class DataRaceTests
{
    [Test]
    public void RaceFact() => DataRaceWorkloads.RaceFact();

    [Test]
    public void CleanFact() => DataRaceWorkloads.CleanFact();
}
