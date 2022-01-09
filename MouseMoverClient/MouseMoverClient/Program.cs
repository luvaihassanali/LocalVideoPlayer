using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace MouseMoverClient
{
    class Program
    {
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
        private const int MOUSEEVENTF_WHEEL = 0x0800;
        private const int SWP_NOSIZE = 0x0001;

        static string serverIp = "192.168.0.181";
        static int serverPort = 3000;
        static bool serverOffline = true;

        static TcpClient client;
        static System.Timers.Timer pollingTimer;

        static void Main(string[] args)
        {
            Console.Title = "Mouse";
            Console.SetWindowSize(60, 20);
            Console.ForegroundColor = ConsoleColor.Green;
            SetWindowPos(MyConsole, 0, 650, 10, 0, 0, SWP_NOSIZE);

            pollingTimer = new System.Timers.Timer(5500);
            pollingTimer.Elapsed += OnTimedEvent;
            pollingTimer.AutoReset = false;

            Log("Starting using ip address: " + serverIp);

            while (!Console.KeyAvailable)
            {
                CheckForServer();
                ConnectToServer();
            }

            Log("Exiting...");

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

            Thread.Sleep(3000);
        }

        static void ConnectToServer()
        {

            Log("Initializing TCP connection");

            try
            {
                try
                {
                    client = new TcpClient();
                    bool success = false;
                    IAsyncResult result = null;

                    result = client.BeginConnect(serverIp, serverPort, null, null);
                    success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(10));

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
                        Thread.Sleep(3000);
                    }
                    catch (System.InvalidOperationException)
                    {
                        Log("Server not ready. Trying again");
                        return;
                    }

                    stream.Write(data, 0, data.Length);
                    Log("Sent: init (zzzz)");
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
                                Log("Ack received");
                                StopTimer();
                                StartTimer();
                            }

                            if (buffer.Contains("ka"))
                            {
                                StopTimer();
                                Log("Sending ok");
                                data = System.Text.Encoding.ASCII.GetBytes("ack");
                                stream = client.GetStream();
                                stream.Write(data, 0, data.Length);
                                StartTimer();
                            }

                            if (!buffer.Contains("ok") && !buffer.Contains("ka"))
                            {
                                MoveMouse(buffer);
                            }
                        }

                        Log("Stream end. Press any key");
                        stream.Close();
                        client.EndConnect(result);
                        client.Close();
                    }
                }
                catch (ArgumentNullException e)
                {
                    Log("ArgumentNullException: " + e);
                }
                catch (SocketException e)
                {
                    Log("SocketException: " + e);
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
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception e)
            {
                Log(e.Message);
            }
        }

        static void CheckForServer()
        {
            Log("Pinging server...");
            serverOffline = true;

            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            options.DontFragment = true;
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"; //32 bytes
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 120;

            while (serverOffline)
            {
                PingReply reply = pingSender.Send(serverIp, timeout, buffer, options);
                if (reply.Status == IPStatus.Success)
                {
                    Log("Ping success");
                    serverOffline = false;
                }
                else
                {
                    Log("Destination host unreachable");
                }

                Thread.Sleep(1000);
            }
        }

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

        static void MoveMouse(string data)
        {
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

            //("before x: " + x);
            //Log("before y: " + y);
            if (x > 490 || y > 490 || x < -490 || y < -490)
            {
                Log("max");
                x = -x;
                y = -y;
            }
            else if ((x > 319 && x < 490) ||
                     (y > 319 && y < 490) ||
                     (x < -319 && x > -490) ||
                     (y < -319 && y > -490))
            {
                Log("higher mid");
                x = -x / 2;
                y = -y / 2;
            }
            else if ((x > 220 && x < 319) ||
                     (y > 200 && y < 319) ||
                     (x < -220 && x > -319) ||
                     (y < -220 && y > -319))
            {
                Log("lower mid");
                x = -x / 4;
                y = -y / 4;
            }
            else if ((x < 220 && x > -220) || (y < 220 && y > -220))
            {
                Log("min");
                x = -x / 8;
                y = -y / 8;
            }
            else
            {
                Log("idk");
            }
            //Log("after x: " + x);
            //Log("after y: " + y);

            if (buttonTwoState == 0)
            {
                DoMouseRightClick();
            }
            else
            {
                Cursor.Position = new System.Drawing.Point(Cursor.Position.X + x, Cursor.Position.Y + y);
                //Log("Mouse position: " + Cursor.Position.ToString());
            }
        }

        //if greater than 490
        // 400 - 490
        // 300 - 400
        // 300 - 200
        // 200 - 100
        // less than 100
        static void DoMouseClick()
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

        static void Log(string message)
        {
            Console.WriteLine("{0}: {1}", DateTime.Now.ToString("0:HH:mm:ss.fff"), message);
        }

    }
}
