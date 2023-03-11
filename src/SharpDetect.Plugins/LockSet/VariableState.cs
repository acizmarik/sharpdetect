// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Plugins.LockSet
{
    public enum VariableState
    {
        /// <summary>
        /// Newly allocated locations
        /// </summary>
        Virgin,
        /// <summary>
        /// Read/Write issued by the first thread
        /// </summary>
        Exclusive,
        /// <summary>
        /// Read by new thread
        /// </summary>
        Shared,
        /// <summary>
        /// Write issued by new thread
        /// </summary>
        SharedModified
    }

}
