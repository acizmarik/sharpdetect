// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Xunit;

namespace SharpDetect.TestUtils.E2E
{
    [CollectionDefinition("E2E")]
    public class E2ETestCollection : ICollectionFixture<CompilationFixture>
    {

    }
}
