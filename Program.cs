using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO; 

namespace TCPCameraStream
{
    class Program
    {
        static bool Running = true;
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            CaptureService service = CaptureService.Create(0);
            TCPServer server = new TCPServer("127.0.0.1", 8080);
            //TCPServer server = new TCPServer("192.168.30.194", 8080);

            service.OnCapture += (s, stream) => {
              //  Console.WriteLine("OnCapture");
                server.JpegStreamBuffer = stream;
              //  Console.WriteLine("END ONCAPTURE");
            };


            service.Start();
            server.Start();
            
            Console.ReadLine(); 
            Running = false;
            service.Stop();

        }
    }
}
