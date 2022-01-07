using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace MouseMoverService
{
    class MouseWorker
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, CallingConvention = System.Runtime.InteropServices.CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        //Mouse actions
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        private const int MOUSEEVENTF_WHEEL = 0x0800;

        private Thread workerThread = null;
        private bool workerThreadRunning = false;
        private int stopTimeout = 10000;
        TcpClient client;
        private System.Timers.Timer pollingTimer;

        private void SetTimer()
        {
            // Create a timer with a two second interval.
            pollingTimer = new System.Timers.Timer(5000);
            // Hook up the Elapsed event for the timer. 
            pollingTimer.Elapsed += OnTimedEvent;
            pollingTimer.AutoReset = true;
            pollingTimer.Enabled = true;
        }


        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {

            Console.WriteLine("{0:HH:mm:ss.fff} keep alive event raised", e.SignalTime);
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] msg = System.Text.Encoding.ASCII.GetBytes("ka");
                stream.Write(msg, 0, msg.Length);
            }
            catch
            {
                //CustomDialog.ShowMessage("Error", "Lost controller connection.", mainForm.Width, mainForm.Height);
                Stop();
                Start();
                pollingTimer.Enabled = false;
                pollingTimer.Stop();
            }
        }

        private void DoWork()
        {
            try
            {
                while (workerThreadRunning)
                {
                    try
                    {
                        // Set the TcpListener on port 13000.
                        Int32 port = 3000;
                        String server = "192.168.0.181";
                        String message = "init";
                        
                        client = new TcpClient();
                        System.IAsyncResult result = client.BeginConnect(server, port, null, null);
                        bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(120));
                        if (!success)
                        {
                            Console.WriteLine("Cannot connect to server. Exiting");
                            Thread.Sleep(3000);
                            System.Environment.Exit(0);
                        }
                        Console.WriteLine("Connected.");
                        Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
                        NetworkStream stream = client.GetStream();
                        stream.Write(data, 0, data.Length);
                        Console.WriteLine("Sent: {0}", message);

                        SetTimer();

                        // Enter the listening loop.
                        while (true)
                        {
                            int i;
                            int counter = 1;

                            Byte[] bytes = new Byte[256];
                            String buffer = null;

                            // Loop to receive all the data sent by the client.
                            while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                            {
                                // Translate data bytes to a ASCII string.
                                buffer = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                                Console.WriteLine("{0}: Received: {1}", counter++, buffer);

                                //MoveMouse(data);

                            }

                            // Shutdown and end connection
                            Console.WriteLine("shutting down. Press a key");
                            //Console.ReadKey();
                            stream.Close();
                            client.Close();
                        }
                    }
                    catch (ArgumentNullException e)
                    {
                        Console.WriteLine("ArgumentNullException: {0}", e);
                    }
                    catch (SocketException e)
                    {
                        Console.WriteLine("SocketException: {0}", e);
                    }
                    finally
                    {
                        if (client != null)
                        {
                            //if (client.Connected)
                            //{
                                client.Close();
                            //}
                            client.Dispose();
                        }
                    }
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

        int ignoreCount = 0;

        private void MoveMouse(string data)
        {
            if (ignoreCount < 2)
            {
                ignoreCount++;
                return;
            }

            string[] dataSplit = data.Split(',');
            int x = Int32.Parse(dataSplit[0]);
            int y = Int32.Parse(dataSplit[1]);
            int buttonState = Int32.Parse(dataSplit[2]);
            int buttonTwoState = Int32.Parse(dataSplit[3].Replace("\r\n", ""));

            if (buttonState == 0)
            {
                DoMouseClick();
                return;
            }

            //adjust for less than -512/512
            x = -x / 4;
            y = -y / 4;

            if (buttonTwoState == 0)
            {
                DoMouseRightClick();
            }
            else
            {
                Cursor.Position = new System.Drawing.Point(Cursor.Position.X + x, Cursor.Position.Y + y);
                Console.WriteLine("Position: " + Cursor.Position.ToString());
            }
        }

        public void DoMouseClick()
        {
            //Call the imported function with the cursor's current position
            uint X = (uint)Cursor.Position.X;
            uint Y = (uint)Cursor.Position.Y;
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
        }

        public void DoMouseRightClick()
        {
            //Call the imported function with the cursor's current position
            uint X = (uint)Cursor.Position.X;
            uint Y = (uint)Cursor.Position.Y;
            mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, X, Y, 0, 0);
        }

        public void Start()
        {
            try
            {
                if (workerThread == null)
                {
                    workerThread = new Thread(new ThreadStart(this.DoWork));
                    //workerThread.Priority = ThreadPriority.Normal;
                    workerThread.IsBackground = true;
                    workerThread.Name = "MouseMoverService thread";
                    workerThreadRunning = true;
                    workerThread.Start();
                }
            }
            catch (Exception e)
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
            if (pollingTimer != null)
            {
                pollingTimer.Stop();
                pollingTimer.Dispose();
            }

            if (client != null)
            {
                //if (client.Connected)
                //{
                    client.Close();
                //}
                client.Dispose();
            }
            if (workerThread != null)
            {
                workerThreadRunning = false;
                Join(stopTimeout, workerThread);

                if (workerThread.IsAlive)
                {
                    StopImmediately();
                }
                workerThread = null;
            }
        }

        public void StopImmediately()
        {
            if (pollingTimer != null)
            {
                pollingTimer.Stop();
                pollingTimer.Dispose();
            }

            if (client != null)
            {
                //if (client.Connected)
                //{
                    client.Close();
                //}
                client.Dispose();
            }

            if (workerThread != null)
            {
                workerThread.Abort();
                workerThread.Join();
                workerThread = null;
            }
        }

        //check if current thread needs to sleep
        public static void Join(int timeoutMs, Thread workerToWatch)
        {
            DateTime endDateTimeOut = DateTime.UtcNow.AddMilliseconds(timeoutMs);

            while (DateTime.UtcNow < endDateTimeOut && workerToWatch.IsAlive)
            {
                Thread.Sleep(100);
            }
        }

        public static IPAddress GetLocalIPAddress()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList[host.AddressList.Length - 1];
            /*foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");*/
        }
    }

}

