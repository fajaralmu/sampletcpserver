using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Drawing;

namespace TCPCameraStream
{
    public class TCPServer
    {
        private readonly IPAddress _ipAddress;
        private readonly int _port;
        private readonly TcpListener _listener;
        private bool _running = false;
        public TCPServer(string ip, int port)
        {
            _ipAddress = IPAddress.Parse(ip);
            _port = port;
            _listener = new TcpListener(_ipAddress, _port);
        }

        public void Start()
        {
            _running = true;
            Task.Run(Run);
        }

        public void Stop()
        {
            _running = false;
            _listener?.Stop();
        }
        public void Run()
        {
            //---listen at the specified IP and port no.---
            Console.WriteLine("starting...");
            _listener.Start();
            Console.WriteLine("Started..");
            while (_running)
            {
                //---incoming client connected---
                TcpClient client = _listener.AcceptTcpClient();


                Thread thread = new Thread(new ThreadStart(() => Process(client)));
                thread.Start();
                Thread.Sleep(1);
            }
        }

        private void Process(TcpClient client)
        {
            Stream inputStream = client.GetStream();
            Stream outputStream = client.GetStream();
            string request = Readline(inputStream);
            Console.WriteLine("TOKEN: " + request);
            Bitmap bmp = new Bitmap("picture.png");
            Bitmap bmp2 = new Bitmap("SampleFont.png");
            byte[] byteImg1 = ImageToByte(bmp);
            byte[] byteImg2 = ImageToByte(bmp2);

            WriteResponse(outputStream, "image/bmp", byteImg1); //Encoding.UTF8.GetBytes("{\"message\":\"Hello World\"}"));   
            
            // for (var i = 0; i < 30; i++)
            // {
            //     byte[] content = i % 2 == 0? byteImg1 : byteImg2;
            //     outputStream.Write(content, 0, content.Length);
            //     Task.Delay(500).Wait();
            // }
            
            outputStream.Flush();
            outputStream.Close();
            outputStream = null;

            inputStream.Close();
            inputStream = null;
        }

        public static byte[] ImageToByte(Image img)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }

        private static void WriteResponse(Stream stream, string contentType, byte[] content)
        {
            StreamWriter writer = new StreamWriter(stream);
            writer.AutoFlush =true;
            IDictionary<string, string> headers = new Dictionary<string, string>()
            {

            };
            headers["Content-Length"] = content.Length.ToString();
            headers["Content-Type"] = contentType;
            headers["Connection"] = "Keep-Alive";
            headers["User"] = "fajar";

            writer.WriteLine(string.Format("HTTP/1.0 {0} {1}", 200, "OK"));
            foreach (var item in headers)
            {
                writer.WriteLine($"{item.Key}: {item.Value}");
            }
            writer.WriteLine("");

            writer.AutoFlush = false;

            stream.Write(content, 0, content.Length);
        }

        private static string Readline(Stream stream)
        {
            int next_char;
            string data = "";
            while (true)
            {
                next_char = stream.ReadByte();
                if (next_char == '\n') { break; }
                if (next_char == '\r') { continue; }
                if (next_char == -1) { Thread.Sleep(1); continue; };
                data += Convert.ToChar(next_char);
            }
            return data;
        }

        private static void Write(Stream stream, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }
        // public override void handleGETRequest(HttpProcessor p)
        // {
        //     Console.WriteLine("request: {0}", p.http_url);
        //     p.writeSuccess();
        //     p.outputStream.WriteLine("<html><body><h1>test server</h1>");
        //     p.outputStream.WriteLine("Current Time: " + DateTime.Now.ToString());
        //     p.outputStream.WriteLine("url : {0}", p.http_url);

        //     p.outputStream.WriteLine("<form method=post action=/form>");
        //     p.outputStream.WriteLine("<input type=text name=foo value=foovalue>");
        //     p.outputStream.WriteLine("<input type=submit name=bar value=barvalue>");
        //     p.outputStream.WriteLine("</form>");
        // }

        // public override void handlePOSTRequest(HttpProcessor p, StreamReader inputData)
        // {
        //     Console.WriteLine("POST request: {0}", p.http_url);
        //     string data = inputData.ReadToEnd();

        //     p.outputStream.WriteLine("<html><body><h1>test server</h1>");
        //     p.outputStream.WriteLine("<a href=/test>return</a><p>");
        //     p.outputStream.WriteLine("postbody: <pre>{0}</pre>", data);
        // }
    }
}