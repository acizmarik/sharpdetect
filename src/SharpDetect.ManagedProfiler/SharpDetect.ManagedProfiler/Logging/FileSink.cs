namespace SharpDetect.Profiler.Logging
{
    internal class FileSink : ILoggerSink
    {
        private readonly object lockObj = new();
        private readonly StreamWriter writer;
        private bool isDisposed;

        public FileSink(string path, bool append = false)
        {
            writer = new StreamWriter(path, append);
        }

        public void Write(string message)
        {
            lock (lockObj)
                writer.Write(message);
        }

        public void WriteLine(string message)
        {
            lock (lockObj)
                writer.WriteLine(message);
        }

        public void Flush()
        {
            writer.Flush();
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                writer.Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }
}
