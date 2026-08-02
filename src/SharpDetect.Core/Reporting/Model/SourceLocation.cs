// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Reporting.Model;

public readonly record struct SourceLocation(string DocumentPath, int Line, SourceCodeSnippet Snippet);
