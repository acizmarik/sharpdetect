namespace SharpDetect.Profiler.Communication
{
    internal interface ICommunicationWorker : IDisposable
    {
        void Start();
        void Terminate();
    }
}
