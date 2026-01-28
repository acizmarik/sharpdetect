// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.E2ETests;

internal class TestTimeProvider(DateTimeOffset dateTimeOffset) : TimeProvider
{
    public override DateTimeOffset GetUtcNow()
    {
        return dateTimeOffset;
    }
}