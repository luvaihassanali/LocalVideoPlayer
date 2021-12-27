using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MouseMoverService.Logging
{
    public sealed class EventLogService
    {
        private static IEventLogService instance;

        public static IEventLogService Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = CreateDefault();
                }

                return instance;
            }
            set
            {
                if(value == null)
                {
                    throw new ArgumentNullException("instance");
                }
                instance = value;
            }
        }

        private static IEventLogService CreateDefault()
        {
            return new NullEventLog();
        }
    }
}
