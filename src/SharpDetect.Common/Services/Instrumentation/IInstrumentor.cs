// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Common.Services.Instrumentation
{
    public interface IInstrumentor
    {
        int InstrumentedMethodsCount { get; }
        int InjectedMethodWrappersCount { get; }
        int InjectedMethodHooksCount { get; }
    }
}
