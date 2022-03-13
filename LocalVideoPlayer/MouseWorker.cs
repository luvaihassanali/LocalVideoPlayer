﻿using System;
using System.Configuration;
using System.IO.Ports;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace LocalVideoPlayer
{
    class MouseWorker
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, CallingConvention = System.Runtime.InteropServices.CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        // Mouse actions
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        private const int MOUSEEVENTF_WHEEL = 0x0800;

        private bool serverIsNotConnected = true;
        private bool workerThreadRunning = false;
        private int joystickX;
        private int joystickY;
        private int timelineShowTimeout = 10000;
        private MainForm mainForm;
        private SerialPort serialPort;
        private System.Timers.Timer pollingTimer;
        private TcpClient client;
        private Thread workerThread = null;

        private string serverIp = "192.168.0.181";
        private int serverPort = 3000;

        public MouseWorker(MainForm m)
        {
            mainForm = m;
            InitializeSerialPort();
        }

        #region Serial port

        private void InitializeSerialPort()
        {
            serialPort = new SerialPort();
            string portNumber = ConfigurationManager.AppSettings["comPort"];
            serialPort.PortName = "COM" + portNumber;
            serialPort.BaudRate = 9600;
            serialPort.DataBits = 8;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            serialPort.Handshake = Handshake.None;
            serialPort.DataReceived += SerialPort_DataReceived;

            try
            {
                serialPort.Open();
                MainForm.Log("Connected to serial port");
            }
            catch
            {
                MainForm.Log("No device connected to serial port");
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort serialPort = (SerialPort)sender;
            if (e.EventType == SerialData.Chars)
            {
                string msg = serialPort.ReadLine();
                MainForm.Log("Serial port received: " + msg);
                if (msg.Contains("stop"))
                {
                    FormCollection formCollection = Application.OpenForms;
                    foreach (Form f_ in formCollection)
                    {
                        if (f_.Name.Equals("PlayerForm"))
                        {
                            PlayerForm pf = (PlayerForm)f_;
                            pf.InitiatePause();
                        }
                    }
                }
            }
        }

        #endregion

        private void DoWork()
        {
            pollingTimer = new System.Timers.Timer(10000);
            pollingTimer.Elapsed += OnTimedEvent;
            pollingTimer.AutoReset = false;

            while (workerThreadRunning)
            {
                CheckForServer();
                ConnectToServer();
            }
        }

        private void StartTimer()
        {
            pollingTimer.Enabled = true;
            pollingTimer.Start();
        }

        private void StopTimer()
        {
            pollingTimer.Enabled = false;
            pollingTimer.Stop();
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Log("Polling timer stopped");
            pollingTimer.Enabled = false;
            pollingTimer.Stop();

            StopImmediately();
            Start();
        }

        private void ConnectToServer()
        {
            Log("Initializing TCP connection");
            try
            {
                client = new TcpClient();
                bool success = false;
                IAsyncResult result = null;

                result = client.BeginConnect(serverIp, serverPort, null, null);
                success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));

                while (!success)
                {
                    Log("Cannot connect to server. Trying again");
                    return;
                }

                Byte[] data = System.Text.Encoding.ASCII.GetBytes("zzzz");
                NetworkStream stream = null;

                try
                {
                    stream = client.GetStream();
                    Log("Connected.");
                    Thread.Sleep(1000);
                }
                catch (System.InvalidOperationException)
                {
                    Log("Server not ready. Trying again");
                    return;
                }

                stream.Write(data, 0, data.Length);
                Log("Sent init");
                StartTimer();

                while (true)
                {
                    int i;
                    Byte[] bytes = new Byte[256];
                    String buffer = null;

                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        buffer = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                        Log("Received: " + buffer.Replace("\r\n", ""));

                        if (buffer.Contains("initack"))
                        {
                            Log("initack received");
                            StopTimer();
                            StartTimer();
                        }

                        if (buffer.Contains("ka"))
                        {
                            StopTimer();
                            Log("Sending ack");
                            data = System.Text.Encoding.ASCII.GetBytes("ack");
                            stream = client.GetStream();
                            stream.Write(data, 0, data.Length);
                            StartTimer();
                        }

                        if (!buffer.Contains("ok") && !buffer.Contains("ka") && !buffer.Contains("initack"))
                        {
                            ParseTcpDataIn(buffer);
                        }
                    }

                    Log("Stream end. Press any key");
                    stream.Close();
                    client.EndConnect(result);
                    client.Close();
                }
            }
            catch (Exception e)
            {
                Log("MouseWorker_ConnectToServerException: " + e.Message);
            }
            finally
            {
                if (client != null)
                {
                    client.Close();
                    client.Dispose();
                }
            }
        }

        private void CheckForServer()
        {
            Log("Pinging server...");
            serverIsNotConnected = true;

            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            options.DontFragment = true;
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"; //32 bytes
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 120;

            while (serverIsNotConnected)
            {
                PingReply reply = null;
                try
                {
                    reply = pingSender.Send(serverIp, timeout, buffer, options);
                }
                catch
                { }

                if (reply != null && reply.Status == IPStatus.Success)
                {
                    Log("Ping success");
                    serverIsNotConnected = false;
                }
                else
                {
                    Log("Destination host unreachable");
                }

                try
                {
                    Thread.Sleep(3000);
                }
                catch
                { }
            }
        }

        private void ParseTcpDataIn(string data)
        {
            string[] dataSplit = data.Split(',');
            if (dataSplit.Length > 5)
            {
                Log("Error. Message incorrect format: " + data);
                return;
            }
            joystickX = Int32.Parse(dataSplit[0]);
            joystickY = Int32.Parse(dataSplit[1]);
            int buttonState = Int32.Parse(dataSplit[2]);
            int scrollState = Int32.Parse(dataSplit[3].Replace("\r\n", ""));

            if (buttonState == 0)
            {
                DoMouseClick();
                return;
            }

            if (scrollState == 0)
            {
                joystickY = joystickY * 2;
                mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)joystickY, 0);
            }
            else
            {
                DoMouseMove();
            }
        }

        void DoMouseMove()
        {
            joystickX = -joystickX;
            joystickY = -joystickY;
            int divisor = 20;
            if ((joystickX > 0 && joystickX < 150) || (joystickX < 0 && joystickX > -150))
            {
                divisor = 60;
            }
            else if ((joystickX > 150 && joystickX < 400) || (joystickX < -150 && joystickX > -400))
            {
                divisor = 40;
            }

            for (int i = 0; i < 15; i++)
            {
                Cursor.Position = new System.Drawing.Point(Cursor.Position.X + joystickX / divisor, Cursor.Position.Y + joystickY / divisor);
                Thread.Sleep(1);
            }

        }

        static public void DoMouseClick()
        {
            uint X = (uint)Cursor.Position.X;
            uint Y = (uint)Cursor.Position.Y;
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
        }

        static public void DoMouseRightClick()
        {
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
                    workerThread.IsBackground = true;
                    workerThread.Name = "LocalVideoPlayer mouse thread";
                    workerThreadRunning = true;
                    workerThread.Start();
                }
            }
            catch (Exception e)
            {
                Log(e.Message);
            }
        }

        public void Stop()
        {
            Stop(timelineShowTimeout);
        }

        public void Stop(int stopTimeout)
        {

            if (serialPort != null)
            {
                serialPort.Close();
                serialPort.Dispose();
            }

            if (pollingTimer != null)
            {
                pollingTimer.Stop();
                pollingTimer.Dispose();
            }

            if (client != null)
            {
                client.Close();
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
                client = null;
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
                client.Close();
                client.Dispose();
            }

            if (workerThread != null)
            {
                workerThread.Abort();
                workerThread.Join();
                workerThread = null;
            }
        }

        // Check if current thread needs to sleep
        public void Join(int timeoutMs, Thread workerToWatch)
        {
            DateTime endDateTimeOut = DateTime.UtcNow.AddMilliseconds(timeoutMs);

            while (DateTime.UtcNow < endDateTimeOut && workerToWatch.IsAlive)
            {
                Thread.Sleep(100);
            }
        }

        public void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine("{0}: {1}", DateTime.Now.ToString("HH:mm:ss.fff"), message);
        }
    }

}
