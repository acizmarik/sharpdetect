using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpDetect.Core
{
    public interface IAnalysis
    {
        public Task<bool> ExecuteAsync(CancellationToken ct);
    }
}
