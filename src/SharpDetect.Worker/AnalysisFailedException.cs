// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Worker;

public sealed class AnalysisFailedException : Exception
{
    private const string DefaultMessage = "Analysis failed unexpectedly";
    
    public AnalysisFailedException()
        : base(DefaultMessage)
    {
        
    }
    
    public AnalysisFailedException(Exception innerException)
        : base(DefaultMessage, innerException)
    {
    }
}