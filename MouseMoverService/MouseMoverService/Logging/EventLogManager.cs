using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MouseMoverService.Logging
{
    public interface IEventLogAccess
    {
        void WriteEntryError(string message);

        void WriteEntryInfo(string message);

        void WriteEntryWarning(string message);
    }

    public sealed class EventLogManager : IEventLogService
    {
        private sealed class InternalEventLog : IEventLogAccess
        {
            private System.Diagnostics.EventLog eventLog = null;
            private string sourceName;
            private string logName;

            public InternalEventLog(string sourceName, string logName)
            {
                this.sourceName = sourceName;
                this.logName = logName;

                //Create source if not exist
                if(!System.Diagnostics.EventLog.SourceExists(sourceName))
                {
                    //Event log source should be created and immediately used
                    //There exists latency time to enable the source, it should be created
                    //prior to executing the application that uses the source.
                    //Execute this sample a second time to use the new source
                    System.Diagnostics.EventLog.CreateEventSource(sourceName, logName);
                }

                eventLog = new System.Diagnostics.EventLog();
                eventLog.Source = sourceName;
            }

            public void WriteEntryError(string message)
            {
                eventLog.WriteEntry(message, System.Diagnostics.EventLogEntryType.Error);
            }

            public void WriteEntryInfo(string message)
            {
                eventLog.WriteEntry(message, System.Diagnostics.EventLogEntryType.Information);
            }

            public void WriteEntryWarning(string message)
            {
                eventLog.WriteEntry(message, System.Diagnostics.EventLogEntryType.Warning);
            }
        }

        private string eventSourceName;
        private string eventLogName;

        private IEventLogAccess eventLogAccess;

        public EventLogManager(string eventLogName, string eventSourceName, IEventLogAccess eventLogAccess)
        {
            this.eventLogName = eventLogName;
            this.eventSourceName = eventSourceName;

            if(eventLogAccess == null)
            {
                this.eventLogAccess = (IEventLogAccess)(new InternalEventLog(eventSourceName, eventLogName));
            }
            else
            {
                this.eventLogAccess = eventLogAccess;
            }
        }

        #region IEventLogService Members
        public void WriteEventError(string message)
        {
            eventLogAccess.WriteEntryError(message);
        }

        public void WriteEventInfo(string message)
        {
            eventLogAccess.WriteEntryInfo(message);
        }

        public void WriteEventWarning(string message)
        {
            eventLogAccess.WriteEntryWarning(message);
        }
        #endregion

        #region InternalEventLog Members
        public string GetServiceName()
        {
            return "EventLogService";
        }

        public void InitalizeService()
        {
            //nothing to do
        }

        public void CleanupService()
        {
            //nothing to do
        }
        #endregion
    }
}
