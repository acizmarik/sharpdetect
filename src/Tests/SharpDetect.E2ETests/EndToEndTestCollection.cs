// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Worker.Configuration;
using Xunit;

namespace SharpDetect.E2ETests;

[CollectionDefinition(DataRacePluginTests.CollectionName)]
public sealed class DataRacePluginTestsCollection : ICollectionFixture<EndToEndCollectionFixture> { }

[CollectionDefinition(DeadlockPluginTests.CollectionName)]
public sealed class DeadlockPluginTestsCollection : ICollectionFixture<EndToEndCollectionFixture> { }

[CollectionDefinition(FieldAccessTests.CollectionName)]
public sealed class FieldAccessTestsCollection : ICollectionFixture<EndToEndCollectionFixture> { }

[CollectionDefinition(MethodInterpretationTests.CollectionName)]
public sealed class MethodInterpretationTestsCollection : ICollectionFixture<EndToEndCollectionFixture> { }

[CollectionDefinition(MultiProcessTests.CollectionName)]
public sealed class MultiProcessTestsCollection : ICollectionFixture<EndToEndCollectionFixture> { }

[CollectionDefinition(ObjectTrackingTests.CollectionName)]
public sealed class ObjectTrackingTestsCollection : ICollectionFixture<EndToEndCollectionFixture> { }

[CollectionDefinition(ShadowCallstackTests.CollectionName)]
public sealed class ShadowCallstackTestsCollection : ICollectionFixture<EndToEndCollectionFixture> { }

public sealed class EndToEndCollectionFixture
{
    public EndToEndCollectionFixture()
    {
        EnvironmentUtils.Initialize();
    }
}
