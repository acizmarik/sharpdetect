using Xunit;

// Since we are using sockets, parallization does not make sense
// Otherwise results in numerous SocketAlreadyInUseExceptions...
[assembly: CollectionBehavior(DisableTestParallelization = true)]