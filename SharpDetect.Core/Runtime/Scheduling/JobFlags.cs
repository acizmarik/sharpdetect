using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpDetect.Core.Runtime.Scheduling
{
    [Flags]
    internal enum JobFlags
    {
        None,
        Concurrent,
        SynchronizedBlocking,
        SynchronizedUnblocking,
        OverrideSuspend
    }
}
