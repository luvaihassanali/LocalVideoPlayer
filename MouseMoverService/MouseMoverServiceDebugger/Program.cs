using System;

namespace MouseMoverService
{
    class Program
    {
        static void Main(string[] args)
        {
            Bootstrap bootstrap = new Bootstrap();

            Console.WriteLine("Starting service...");
            bootstrap.StartService();

            Console.WriteLine("Press Enter key to stop the service");
            Console.ReadLine();

            Console.WriteLine("Stopping service...");
            bootstrap.StopService();

            Console.WriteLine("Stopped");
            System.Threading.Thread.Sleep(3000);
        }
    }
}
