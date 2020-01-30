using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AARC.Utilities
{
    public class TimeProcess : IDisposable
    {
        private readonly IAarcLogger _log;
        private readonly DateTime _start;
        public TimeSpan TimeTaken { get; private set; }
        private readonly string _srccode;
        public TimeProcess(IAarcLogger log, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            _log = log;
            _start = DateTime.Now;
            _srccode = $"{caller}";
        }
        public void Dispose()
        {
             TimeTaken = DateTime.Now - _start;
            _log?.Info($"{_srccode} Process took {TimeTaken.TotalMilliseconds} ms");
        }
    }
}
