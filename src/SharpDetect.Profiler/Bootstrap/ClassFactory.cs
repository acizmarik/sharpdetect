using System.Runtime.InteropServices;

namespace SharpDetect.Profiler
{
    internal class ClassFactory : IClassFactory
    {
        public NativeObjects.IClassFactory Object { get; private set; }
        private CorProfilerCallback callback = null!;

        public ClassFactory()
        {
            Object = NativeObjects.IClassFactory.Wrap(this);
        }

        public int QueryInterface(in Guid guid, out IntPtr ptr)
        {
            ptr = IntPtr.Zero;
            return 0;
        }

        public int AddRef()
        {
            return 1;
        }

        public int Release()
        {
            return 1;
        }

        public int CreateInstance(IntPtr outer, in Guid guid, out IntPtr instance)
        {
            callback = new CorProfilerCallback();

            instance = callback.Object;

            return 0;
        }

        public int LockServer(bool @lock)
        {
            return 0;
        }
    }
}
