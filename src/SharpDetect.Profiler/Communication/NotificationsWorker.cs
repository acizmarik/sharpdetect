namespace SharpDetect.Profiler.Communication
{
    internal class NotificationsWorker : RelayWorkerBase
    {
        public const string InternalNotificationsConnectionString = "inproc://profiling-notifications";

        public NotificationsWorker(string connectionString)
            : base(connectionString, InternalNotificationsConnectionString)
        {

        }
    }
}
