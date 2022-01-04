using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO; 

namespace TCPCameraStream
{
    class Program
    {
        static void Main(string[] args)
        {
            RunServer();
        }

        private static void RunServer()
        {
            Console.WriteLine("Hello World!");
            ICaptureService capture     = GetCaptureService();
            TCPServer server            = new TCPServer("0.0.0.0", 8080);

            capture.OnCapture += (s, stream) => {
                server.JpegStreamBuffer = stream;
            };

            capture.Start();
            server.Start();
            
            Console.ReadLine(); 
            capture.Stop();

        }

        private static ICaptureService GetCaptureService()
        {
            if (true)
            {
                return new CaotureServiceTcp("stampede-dev00.local", 5001);
            }
            return CaptureService.Create(0);
        }
    }
}
