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
            Console.WriteLine("Hello World!");
            CaptureService capture  = CaptureService.Create(0);
            TCPServer server        = new TCPServer("0.0.0.0", 8080);

            capture.OnCapture += (s, stream) => {
                server.JpegStreamBuffer = stream;
            };

            capture.Start();
            server.Start();
            
            Console.ReadLine(); 
            capture.Stop();

        }
    }
}
