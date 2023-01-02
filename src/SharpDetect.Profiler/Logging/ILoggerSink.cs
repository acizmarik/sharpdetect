namespace SharpDetect.Profiler.Logging
{
    internal interface ILoggerSink : IDisposable
    {
        void Write(string message);
        void WriteLine(string message);
        void Flush();
    }
}
