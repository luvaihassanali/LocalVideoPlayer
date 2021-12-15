using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace LocalVideoPlayer
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

        private Thread workerThread = null;
        private bool workerThreadRunning = false;
        private int stopTimeout = 10000;
        private MainForm mainForm;
        private PictureBox remotePictureBox;
        TcpListener server;
        TcpClient client;
        private System.Timers.Timer pollingTimer;

        public MouseWorker(MainForm m)
        {
            mainForm = m;
            foreach (Control c in mainForm.Controls)
            {
                if (c.Name.Equals("remotePictureBox"))
                {
                    remotePictureBox = c as PictureBox;
                }
            }
        }

        private void SetTimer()
        {
            // Create a timer with a two second interval.
            pollingTimer = new System.Timers.Timer(1000);
            // Hook up the Elapsed event for the timer. 
            pollingTimer.Elapsed += OnTimedEvent;
            pollingTimer.AutoReset = true;
            pollingTimer.Enabled = true;
        }


        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {

            //Console.WriteLine("{0:HH:mm:ss.fff} event raised: {1}", e.SignalTime, client.Connected);
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
                    server = null;
                    try
                    {
                        // Set the TcpListener on port 13000.
                        Int32 port = 3000;
                        IPAddress localAddr = GetLocalIPAddress();
                        Console.WriteLine("Starting server on " + localAddr.ToString());
                        // TcpListener server = new TcpListener(port);
                        server = new TcpListener(localAddr, port);

                        // Start listening for client requests.
                        server.Start();

                        // Buffer for reading data
                        Byte[] bytes = new Byte[256];
                        String data = null;

                        // Enter the listening loop.
                        while (true)
                        {
                            data = null;

                            if (remotePictureBox.InvokeRequired)
                            {
                                remotePictureBox.BeginInvoke(new MethodInvoker(delegate { remotePictureBox.Image = Properties.Resources.ripple_red; }));
                            }
                            else
                            {
                                remotePictureBox.Image = Properties.Resources.ripple_red;
                            }
                            // Perform a blocking call to accept requests.
                            // You could also use server.AcceptSocket() here.
                            Console.WriteLine("Waiting for a connection... ");
                            //For intial load and close without connection add try/catch
                            client = server.AcceptTcpClient();
                            Console.WriteLine("Connected!");

                            SetTimer();

                            if (remotePictureBox.InvokeRequired)
                            {
                                remotePictureBox.BeginInvoke(new MethodInvoker(delegate { remotePictureBox.Image = Properties.Resources.ripple_green; }));
                            }
                            else
                            {
                                remotePictureBox.Image = Properties.Resources.ripple_green;
                            }

                            // Get a stream object for reading and writing
                            NetworkStream stream = client.GetStream();

                            int i;
                            int counter = 1;

                            // Loop to receive all the data sent by the client.
                            while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                            {
                                // Translate data bytes to a ASCII string.
                                data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                                Console.WriteLine("{0}: Received: {1}", counter++, data);

                                MoveMouse(data);

                                byte[] msg = System.Text.Encoding.ASCII.GetBytes("OK");
                                stream.Write(msg, 0, msg.Length);
                                //Console.WriteLine("Sending back: {0}", data);
                            }

                            // Shutdown and end connection
                            client.Close();
                        }
                    }
                    catch (SocketException e)
                    {
                        Console.WriteLine("SocketException: {0}", e);
                    }
                    finally
                    {
                        if (client != null)
                        {
                            if (client.Connected)
                            {
                                client.Close();
                            }
                            client.Dispose();
                        }
                        // Stop listening for new clients.
                        if (server != null)
                        {
                            server.Stop();
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

        private void MoveMouse(string data)
        {
            string[] dataSplit = data.Split(',');
            int x = Int32.Parse(dataSplit[0]);
            int y = Int32.Parse(dataSplit[1]);
            int buttonState = Int32.Parse(dataSplit[2].Replace("\r\n", ""));

            if (buttonState == 0)
            {
                DoMouseClick();
                return;
            }

            //adjust for less than -512/512
            x = x / 4;
            y = y / 4;

            if (mainForm.InvokeRequired)
            {
                //To-do: Go over invokes one more time
                //To-do: Create polling timer to detect disconnect
                mainForm.BeginInvoke(new MethodInvoker(delegate
                {
                    mainForm.Cursor = new Cursor(Cursor.Current.Handle);
                    Cursor.Position = new System.Drawing.Point(Cursor.Position.X + x, Cursor.Position.Y + y);
                    
                }));
            }
            else
            {
                mainForm.Cursor = new Cursor(Cursor.Current.Handle);
                Cursor.Position = new System.Drawing.Point(Cursor.Position.X + x, Cursor.Position.Y + y);
            }
        }

        public void DoMouseClick()
        {
            //Call the imported function with the cursor's current position
            uint X = (uint)Cursor.Position.X;
            uint Y = (uint)Cursor.Position.Y;
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
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
                    workerThread.Name = "LocalVideoPlayer mouse thread";
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
            if (remotePictureBox.InvokeRequired)
            {
                remotePictureBox.BeginInvoke(new MethodInvoker(delegate { remotePictureBox.Image = Properties.Resources.ripple_red; }));
            }
            else
            {
                remotePictureBox.Image = Properties.Resources.ripple_red;
            }

            if (pollingTimer != null)
            {
                pollingTimer.Stop();
                pollingTimer.Dispose();
            }

            if (client != null)
            {
                if (client.Connected)
                {
                    client.Close();
                }
                client.Dispose();
            }

            if (server != null)
            {
                server.Stop();
                server = null;
            }

            if (workerThread != null)
            {
                workerThreadRunning = false;
                Join(stopTimeout, workerThread);

                if (workerThread.IsAlive)
                {
                    StopImmediately();
                }
                server = null;
                workerThread = null;
            }
        }

        public void StopImmediately()
        {
            if (remotePictureBox.InvokeRequired)
            {
                remotePictureBox.BeginInvoke(new MethodInvoker(delegate { remotePictureBox.Image = Properties.Resources.ripple_red; }));
            }
            else
            {
                remotePictureBox.Image = Properties.Resources.ripple_red;
            }

            if (pollingTimer != null)
            {
                pollingTimer.Stop();
                pollingTimer.Dispose();
            }

            if (client != null)
            {
                if (client.Connected)
                {
                    client.Close();
                }
                client.Dispose();
            }

            if (server != null)
            {
                server.Stop();
                server = null;
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

