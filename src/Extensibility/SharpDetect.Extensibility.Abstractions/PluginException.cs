// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿namespace SharpDetect.Extensibility.Abstractions;

public class PluginException(string message, Exception? innerException = null)
    : Exception(message, innerException)
{
}
