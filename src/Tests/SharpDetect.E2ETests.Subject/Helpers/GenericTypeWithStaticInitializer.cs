// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.E2ETests.Subject.Helpers.DataRaces
{
    public class GenericTypeWithStaticInitializer<T>
    {
        public static HashSet<string> Values = new(StringComparer.OrdinalIgnoreCase)
        {
            "first",
            "second"
        };
    }
}
