using MouseMoverService.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MouseMoverService
{
    public sealed class Worker
    {
        private Thread workerThread = null;
        private bool workerThreadRunning = false;
        private int sleepTime = 5;
        private int stopTimeout = 10000;

        public Worker()
        {
            //register event handler
        }

        private void DoWork()
        {
            EventLogService.Instance.WriteEventInfo("DoWork started");
            try
            {
                while(workerThreadRunning)
                {
                    EventLogService.Instance.WriteEventInfo("WORK");
                    Thread.Sleep(sleepTime * 1000);
                }
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void Start()
        {
            try
            {
                if(workerThread == null)
                {
                    workerThread = new Thread(new ThreadStart(this.DoWork));
                    workerThread.Priority = ThreadPriority.Lowest;
                    workerThread.IsBackground = true;
                    workerThread.Name = "MouseMoverService worker thread";
                    workerThreadRunning = true;
                    workerThread.Start();
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void Stop()
        {
            Stop(stopTimeout);
        }

        public void Stop(int stopTimeout)
        {
            if(workerThread != null)
            {
                workerThreadRunning = false;
                Join(stopTimeout, workerThread);

                if(workerThread.IsAlive)
                {
                    StopImmediately();
                }

                workerThread = null;
            }
        }

        public void StopImmediately()
        {
            if(workerThread != null)
            {
                workerThread.Abort();
                workerThread.Join();
                workerThread = null;
            }
        }

        //check if current thread needs to sleep
        public static void Join(int timeoutMs, Thread workerToWatch)
        {
            DateTime endDateTimeOut = SystemTime.UtcNow.AddMilliseconds(timeoutMs);

            while(SystemTime.UtcNow < endDateTimeOut && workerToWatch.IsAlive)
            {
                Thread.Sleep(100);
            }
        }
    }

    //This class is used to abstract DateTime's Now and UtcNow properties. This eases unit testing of time dependent methods
    public static class SystemTime
    {
        public static Func<DateTime> DateTimeNow = () => DateTime.Now;
        public static Func<DateTime> DateTimeUtcNow = () => DateTime.UtcNow;

        public static DateTime Now { get { return SystemTime.DateTimeNow();  } }

        public static DateTime UtcNow { get { return SystemTime.DateTimeUtcNow();  } }

        public static void Reset() { DateTimeNow = () => DateTime.Now;  }
    }
}
