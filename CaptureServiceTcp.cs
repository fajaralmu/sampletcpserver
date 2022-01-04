using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TCPCameraStream
{
    public class CaotureServiceTcp : ICaptureService
    {
        bool Running = true;
        string Data = "";
        readonly IList<byte> Buffer = new List<byte>();
        public EventHandler<byte[]> OnCapture { get; set; }
        private readonly string _host;
        private readonly int _port;
        public CaotureServiceTcp(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public void Start()
        {
            Task.Run(Run);
        }

        public void Run()
        {
            try
            {

                TcpClient tcpclnt = new TcpClient();
                Console.WriteLine("Connecting.....");

                tcpclnt.Connect(_host, _port);
                // use the ipaddress as in the server program

                Console.WriteLine("Connected");
                // Console.Write("Enter the string to be transmitted : ");

                // Task.Run(()=>TransmitThread(tcpclnt));
                Task.Run(() => ReceiveThread(tcpclnt));

                Console.ReadLine();
                Running = false;
                tcpclnt.Close();
                //File.WriteAllText("output.txt", Data);
                Console.WriteLine("Stopped");

            }

            catch (Exception e)
            {
                Console.WriteLine("Error..... " + e.StackTrace);
            }
        }
        

        private void ReceiveThread(TcpClient tcpclnt)
        {
            Buffer.Clear();
            int currentChar = tcpclnt.GetStream().ReadByte();
            Buffer.Add((byte)currentChar);
            while (currentChar != -1 && Running)
            {

                int chr = tcpclnt.GetStream().ReadByte();
                if (currentChar == 255 && chr == 217)
                {
                    Buffer.Add((byte)currentChar);
                    currentChar = chr;
                    OnCapture?.Invoke(null, Buffer.ToArray());
                    Buffer.Clear();
                    continue;
                }
                currentChar = chr;
                Buffer.Add((byte)currentChar);
            }
        }

        public void Stop()
        {
            Running = false;
        }
    }
}
