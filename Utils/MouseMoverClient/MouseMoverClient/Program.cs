﻿using System;
using System.Configuration;
using System.IO.Ports;
using System.Management.Automation;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace MouseMoverClient
{
    class Program
    {
        #region Dll Import 

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        private static IntPtr MyConsole = GetConsoleWindow();

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, CallingConvention = System.Runtime.InteropServices.CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        private const int SWP_NOSIZE = 0x0001;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const uint LWA_ALPHA = 0x2;

        #endregion

        static string serverIp = ConfigurationManager.AppSettings["espIp"];
        static int serverPort = 3000;
        static bool serverIsNotConnected = true;
        static int joystickX;
        static int joystickY;
        static TcpClient client;
        static System.Timers.Timer pollingTimer;
        static SerialPort serialPort;

        static unsafe void Main(string[] args)
        {
            #region Initialize
            Console.Title = "";
            Console.SetWindowSize(60, 10);
            Console.SetBufferSize(60, 10);
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.Clear();

            SetWindowPos(MyConsole, 0, 625, 10, 0, 0, SWP_NOSIZE);
            ConsoleHelper.SetCurrentFont("Cascadia Code", 24);
            // https://stackoverflow.com/questions/24110600/transparent-console-dllimport
            SetWindowLong(MyConsole, GWL_EXSTYLE, GetWindowLong(MyConsole, GWL_EXSTYLE) | WS_EX_LAYERED);
            // Opacity = 0.5 = (255/2) = 128
            SetLayeredWindowAttributes(MyConsole, 0, 128, LWA_ALPHA);

            pollingTimer = new System.Timers.Timer(6000);
            pollingTimer.Elapsed += OnTimedEvent;
            pollingTimer.AutoReset = false;

            #endregion

            Log("Starting using ip address: " + serverIp);
            InitializeSerialPort();
            while (!Console.KeyAvailable)
            {
                CheckForServer();
                ConnectToServer();
            }

            Log("Exiting...");

            #region Clean up


            if (client != null)
            {
                client.Close();
                client.Dispose();
            }

            if (pollingTimer != null)
            {
                pollingTimer.Stop();
                pollingTimer.Dispose();
            }

            #endregion

            Thread.Sleep(1000);
        }

        #region Serial port

        static public void InitializeSerialPort()
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
                Log("Connected to serial port");
            }
            catch
            {
                Log("No device connected to serial port");
            }
        }

        static private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort serialPort = (SerialPort)sender;
            if (e.EventType == SerialData.Chars)
            {
                string msg = serialPort.ReadLine();
                msg = msg.Replace("\r", "");
                switch (msg)
                {
                    case "power":
                        // Send cursor to centre of screen
                        Cursor.Position = new System.Drawing.Point(960, 540);
                        DoMouseDoubleClick();
                        string launchMsg = @"
   ██╗      █████╗ ██╗   ██╗███╗   ██╗ ██████╗██╗  ██╗██╗
   ██║     ██╔══██╗██║   ██║████╗  ██║██╔════╝██║  ██║██║
   ██║     ███████║██║   ██║██╔██╗ ██║██║     ███████║██║
   ██║     ██╔══██║██║   ██║██║╚██╗██║██║     ██╔══██║╚═╝
   ███████╗██║  ██║╚██████╔╝██║ ╚████║╚██████╗██║  ██║██╗
   ╚══════╝╚═╝  ╚═╝ ╚═════╝ ╚═╝  ╚═══╝ ╚═════╝╚═╝  ╚═╝╚═╝
";
                        Console.WriteLine(launchMsg);
                        var script = "Start-ScheduledTask -TaskName \"LocalVideoPlayer\"";
                        var powerShell = PowerShell.Create().AddScript(script);
                        powerShell.Invoke();
                        break;
                    default:
                        Log("Unknown msg received: " + msg);
                        break;
                }
            }
        }

        static private void CheckSerialConnection()
        {
            if (serialPort != null)
            {
                if (!serialPort.IsOpen)
                {
                    try
                    {
                        serialPort.Open();
                        Log("Reconnected to serial port");
                    }
                    catch
                    {
                        Log("Serial port disconnected");
                    }
                }
            }
        }

        #endregion

        #region Timer 

        static void StartTimer()
        {
            pollingTimer.Enabled = true;
            pollingTimer.Start();
        }

        static void StopTimer()
        {
            pollingTimer.Enabled = false;
            pollingTimer.Stop();
        }

        static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Log("Polling timer stopped");
            pollingTimer.Enabled = false;
            pollingTimer.Stop();

            System.Diagnostics.Process.Start(Application.ExecutablePath);
            Environment.Exit(0);
        }

        #endregion

        #region Tcp server

        static void ConnectToServer()
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
                Log("Sent: init");
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
                            // Send cursor to centre of screen
                            Cursor.Position = new System.Drawing.Point(960, 540);
                            DoMouseDoubleClick();
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
                Log("ConnectToServerException: " + e.Message);
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

        static void CheckForServer()
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

                if (reply.Status == IPStatus.Success)
                {
                    Log("Ping success");
                    serverIsNotConnected = false;
                }
                else
                {
                    Log("Destination host unreachable");
                    CheckSerialConnection();
                }

                try
                {
                    Thread.Sleep(1000);
                }
                catch
                { }
            }
        }

        static void ParseTcpDataIn(string data)
        {
            string[] dataSplit = data.Split(',');
            if (dataSplit.Length > 6)
            {
                Log("Error. Message incorrect format: " + data);
                return;
            }
            joystickX = Int32.Parse(dataSplit[0]);
            joystickY = Int32.Parse(dataSplit[1]);
            int buttonState = Int32.Parse(dataSplit[2]);
            int buttonTwoState = Int32.Parse(dataSplit[4].Replace("\r\n", ""));
            int buttonThreeState = Int32.Parse(dataSplit[3].Replace("\r\n", ""));

            if (buttonTwoState == 0 && buttonThreeState == 0)
            {
                System.Diagnostics.Process.Start("taskmgr.exe");
            }

            if (buttonState == 0 || buttonThreeState == 0)
            {
                DoMouseDoubleClick();
                return;
            }

            if (buttonTwoState == 0)
            {
                DoMouseRightClick();
                return;
            }

            DoMouseMove();
        }

        #endregion

        #region Mouse functions

        static void DoMouseMove()
        {
            //joystickX = -joystickX;
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

        static void DoMouseDoubleClick()
        {
            uint X = (uint)Cursor.Position.X;
            uint Y = (uint)Cursor.Position.Y;
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
        }

        static void DoMouseRightClick()
        {
            uint X = (uint)Cursor.Position.X;
            uint Y = (uint)Cursor.Position.Y;
            mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, X, Y, 0, 0);
        }

        #endregion

        static void Log(string message)
        {
            Console.WriteLine("{0}: {1}", DateTime.Now.ToString("> HH:mm:ss.fff"), message);
        }
    }

    // https://stackoverflow.com/questions/6554536/possible-to-get-set-console-font-size-in-c-sharp-net#:~:text=After%20running%20the%20application%20(Ctrl,option%20to%20adjust%20the%20size.
    #region ConsoleHelper

    public static class ConsoleHelper
    {
        private const int FixedWidthTrueType = 54;
        private const int StandardOutputHandle = -11;

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetStdHandle(int nStdHandle);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetCurrentConsoleFontEx(IntPtr hConsoleOutput, bool MaximumWindow, ref FontInfo ConsoleCurrentFontEx);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool GetCurrentConsoleFontEx(IntPtr hConsoleOutput, bool MaximumWindow, ref FontInfo ConsoleCurrentFontEx);

        private static readonly IntPtr ConsoleOutputHandle = GetStdHandle(StandardOutputHandle);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct FontInfo
        {
            internal int cbSize;
            internal int FontIndex;
            internal short FontWidth;
            public short FontSize;
            public int FontFamily;
            public int FontWeight;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string FontName;
        }

        public static FontInfo[] SetCurrentFont(string font, short fontSize = 0)
        {
            FontInfo before = new FontInfo
            {
                cbSize = Marshal.SizeOf<FontInfo>()
            };

            if (GetCurrentConsoleFontEx(ConsoleOutputHandle, false, ref before))
            {

                FontInfo set = new FontInfo
                {
                    cbSize = Marshal.SizeOf<FontInfo>(),
                    FontIndex = 0,
                    FontFamily = FixedWidthTrueType,
                    FontName = font,
                    FontWeight = 400,
                    FontSize = fontSize > 0 ? fontSize : before.FontSize
                };

                // Get some settings from current font.
                if (!SetCurrentConsoleFontEx(ConsoleOutputHandle, false, ref set))
                {
                    var ex = Marshal.GetLastWin32Error();
                    Console.WriteLine("Set error " + ex);
                    throw new System.ComponentModel.Win32Exception(ex);
                }

                FontInfo after = new FontInfo
                {
                    cbSize = Marshal.SizeOf<FontInfo>()
                };
                GetCurrentConsoleFontEx(ConsoleOutputHandle, false, ref after);

                return new[] { before, set, after };
            }
            else
            {
                var er = Marshal.GetLastWin32Error();
                Console.WriteLine("Get error " + er);
                throw new System.ComponentModel.Win32Exception(er);
            }
        }
    }

    #endregion
}
