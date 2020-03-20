using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackBank.Internal
{
    public class TraceIdentifierProviderExtension
    {
        public IDisposable SetTraceIdScope(string traceId)
        {
            return LogScope.Push("TraceId", traceId);
        }
    }
}
