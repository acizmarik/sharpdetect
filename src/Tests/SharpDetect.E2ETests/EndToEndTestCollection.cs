// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Worker.Configuration;
using Xunit;

namespace SharpDetect.E2ETests;

[CollectionDefinition(Name)]
public sealed class EndToEndTestCollection : ICollectionFixture<EndToEndTestCollection.Fixture>
{
    public const string Name = "E2E";
    
    private class Fixture
    {
        public Fixture()
        {
            EnvironmentUtils.Initialize();
        }
    }
}