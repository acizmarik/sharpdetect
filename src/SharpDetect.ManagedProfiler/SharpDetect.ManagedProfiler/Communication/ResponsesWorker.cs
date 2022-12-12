namespace SharpDetect.Profiler.Communication
{
    internal class ResponsesWorker : RelayWorkerBase
    {
        public const string InternalResponsesConnectionString = "inproc://profiling-responses";

        public ResponsesWorker(string connectionString)
            : base(connectionString, InternalResponsesConnectionString)
        {

        }
    }
}
