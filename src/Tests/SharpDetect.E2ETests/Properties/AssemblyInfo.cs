// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Xunit;

// Since we are using sockets, parallization does not make sense
// Otherwise results in numerous SocketAlreadyInUseExceptions...
[assembly: CollectionBehavior(DisableTestParallelization = true)]