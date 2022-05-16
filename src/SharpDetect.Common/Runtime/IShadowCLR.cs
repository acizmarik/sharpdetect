using SharpDetect.Common.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpDetect.Common.Runtime
{
    public interface IShadowCLR
    {
        int ProcessId { get; }
        ShadowRuntimeState State { get; }
        COR_PRF_SUSPEND_REASON? SuspensionReason { get; }
    }
}
