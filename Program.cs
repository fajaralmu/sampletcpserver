using System;

namespace TCPCameraStream
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            CaptureService service = new CaptureService(0);
            service.Start();
            TCPServer server = new TCPServer("127.0.0.1", 8080);
            server.Start();

            Console.ReadLine();

            service.Stop();
        }
    }
}
