// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿namespace SharpDetect.Core.Plugins;

public class PluginException(string message, Exception? innerException = null)
    : Exception(message, innerException)
{
}
