using MouseMoverService.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MouseMoverService
{
    public sealed class Bootstrap
    {
        private bool terminate = false;
        private MouseWorker worker = null;
        private int stopTimeout = 10000;
        private Thread startupThread = null;

        public Bootstrap()
        {
            //setup stuff

            //init logging
            EventLogService.Instance = new EventLogManager("Luvai", "Mouse Mover Service", null);

            //app settings?
        }

        private void Startup()
        {
            // do some initial work

            // wait for required services (loop through process and check if exists)

            try
            {
                bool passed = true; //WaitForRequiredServices();
                if(!terminate && passed)
                {
                    worker = new MouseWorker();
                    worker.Start();
                }
            }
            catch(Exception e)
            {
                EventLogService.Instance.WriteEventError("Failed to start service: " + e.Message);
            }
        }

        public void StartService()
        {
            EventLogService.Instance.WriteEventInfo("Starting MouseMoverService");
            terminate = false;

            startupThread = new Thread(new ThreadStart(Startup));
            startupThread.Name = "MouseMoverService Startup Thread";
            startupThread.IsBackground = true;
            startupThread.Start();
        }

        public void StopService()
        {
            EventLogService.Instance.WriteEventWarning("Stopping MouseMoverService");
            try
            {
                if (!IsStartupTerminated())
                {
                    bool result = true;
                    terminate = true;
                    result = startupThread.Join(stopTimeout); //wait 10 seconds max
                    if (!result)
                    {
                        EventLogService.Instance.WriteEventError("Startup termination timeout occurred");
                    }
                    startupThread = null;
                }

                if(worker != null)
                {
                    worker.Stop();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public bool IsStartupTerminated()
        {
            return (startupThread == null || !startupThread.IsAlive);
        }
    }
}
