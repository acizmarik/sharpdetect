// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Configuration;

public interface IPluginOptionsConfig<out TSelf> where TSelf : IPluginOptionsConfig<TSelf>
{
    static abstract TSelf Default { get; }
}