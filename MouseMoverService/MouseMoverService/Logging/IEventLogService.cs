using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MouseMoverService.Logging
{
    public interface IEventLogService
    {
        void WriteEventError(string message);

        void WriteEventInfo(string message);

        void WriteEventWarning(string message);
    }
}
