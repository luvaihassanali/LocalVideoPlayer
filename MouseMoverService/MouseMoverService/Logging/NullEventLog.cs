using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MouseMoverService.Logging
{
    public sealed class NullEventLog : IEventLogService
    {
        public void WriteEventError(string message)
        {

        }

        public void WriteEventInfo(string message)
        {

        }

        public void WriteEventWarning(string message)
        {

        }
    }
}
