using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TCPCameraStream
{
    public class TCPServer
    {
        private readonly IPAddress _ipAddress;
        private readonly int _port;
        private readonly TcpListener _listener;
        private bool _running = false;
        private byte[] _jpegStreamBuffer;
        public byte[] JpegStreamBuffer 
        {
            get => _jpegStreamBuffer;
            set => _jpegStreamBuffer = value;
        }
        Bitmap bmp ;
        Bitmap bmp2;
        byte[] byteImg1;
        byte[] byteImg2;

        public TCPServer(string ip, int port)
        {
            
            _ipAddress = IPAddress.Parse(ip);
            _port = port;
            _listener = new TcpListener(_ipAddress, _port);

            Task[] tasks = new Task[2];
            tasks[0] = Task.Run(() => bmp = GetBmp("http://localhost/img/picture.png"));
            tasks[1] = Task.Run(() => bmp2 = GetBmp("http://localhost/img/SampleFont.png"));
            Task.WaitAll(tasks);
            byteImg1 = ImageToByte(bmp);
            byteImg2 = ImageToByte(bmp2);

            
        }

        private Bitmap GetBmp(string url)
        {
            WebRequest request      = WebRequest.Create(url);
            WebResponse response    = request.GetResponse();
            Stream responseStream   = response.GetResponseStream();
            return new Bitmap(responseStream);
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
            LogMessage("starting...");
            _listener.Start();
            LogMessage("Started..");
            int counter = 0;
            while (_running)
            {
                //---incoming client connected---
                Socket client = _listener.AcceptSocket();
                counter++;
                Thread thread = new Thread(new ThreadStart(() =>
                {
                    try
                    {
                        LogMessage($"*******************ACCEPT REQUEST {counter}*******************");
                        Process(client);
                        LogMessage($"*******************END REQUEST {counter}*******************");
                    }
                    catch (Exception e)
                    {
                        LogMessage($"Error processing request {counter}: " + e.Message);
                    }
                    LogMessage($"*******************CLOSE CONNECTION {counter}*******************");
                    client.Close();
                }));
                thread.Start();
                Thread.Sleep(1); 
                
            }
        }

        private void Process(Socket client)
        {   
            Stream stream = new NetworkStream(client);
            TcpRequest request = GetRequest(stream);
            LogMessage("REQUEST_RAW:\n" + request.RequestInfo);

            StreamWriter writer = new StreamWriter(stream);

            
            if (request.Path.Equals("/favicon.ico"))
            {
                WriteResponse(writer, "image/bmp", byteImg1); //Encoding.UTF8.GetBytes("{\"message\":\"Hello World\"}"));   
            }
            else if (request.Path.Equals("/video"))
            {
                HandleVideo(writer, request);
            }
            else if (request.Path.Equals("/mjpeg"))
            {
                HandleMJpeg(writer, client);
                return;
            }
            else
            {
                WriteResponse(writer, "text/html", Encoding.UTF8.GetBytes("<b>Hello, world</b>"));
            }

            // for (var i = 0; i < 30; i++)
            // {
            //     byte[] content = i % 2 == 0? byteImg1 : byteImg2;
            //     outputStream.Write(content, 0, content.Length);
            //     Task.Delay(500).Wait();
            // } 

            LogMessage("End of process");
        }

        private void HandleMJpeg(StreamWriter writer, Socket client)
        { 
            MjpegHttpStreamer mjpegHttpStreamer = new MjpegHttpStreamer(writer.BaseStream);


            mjpegHttpStreamer.WriteMJpegHeader();
            LogMessage("MJPEG HTTPHeader sent. Now streaming JPEGs.");
            LogMessage($"_running: {_running}, _jpegStreamBuffer: {_jpegStreamBuffer != null}");
            try
            {
                int lastStreamHash = 0; 
                 
                while (_running && _jpegStreamBuffer != null)
                {
                    int streamHash = _jpegStreamBuffer.GetHashCode();

                    if (streamHash == lastStreamHash)
                    {
                        Thread.Sleep(50);
                        continue;
                    }
                    lastStreamHash = streamHash;

                    if (_jpegStreamBuffer != null)
                    {
                        try {
                           
                            mjpegHttpStreamer.WriteJpeg(_jpegStreamBuffer);
                            
                             
                             
                        } catch (Exception e)
                        {
                            LogMessage("ERROR HandleMJpeg: "+e.Message);
                            LogMessage($"client.Bound: {client.IsBound}");
                            LogMessage($"client.Available: {client.Available}");
                            LogMessage($"client.Connected: {client.Connected}");
                            throw;
                            break;
                        }
                    }
                }
                
            }
            catch (Exception ex)
            {
                LogMessage("MJPEG HTTP Stream ended." + ex.ToString());
            }
            LogMessage("MJPEG HTTP Stream ended");
           

        }

        private void HandleVideo(StreamWriter writer, TcpRequest p)
        {
            CaptureService svc = CaptureService.Instance;
             

            using (FileStream fs = new FileStream($"capture1.mp4", FileMode.Open))
            {
                int startByte = -1;
                int endByte = -1;
                if (p.Headers.ContainsKey("Range"))
                {
                    string rangeHeader = p.Headers["Range"].ToString().Replace("bytes=", "");
                    string[] range = rangeHeader.Split('-');
                    startByte = int.Parse(range[0]);
                    if (range[1].Trim().Length > 0) int.TryParse(range[1], out endByte);
                    if (endByte == -1) endByte = (int)fs.Length;
                }
                else
                {
                    startByte = 0;
                    endByte = (int)fs.Length;
                }
                byte[] buffer = new byte[endByte - startByte];
                fs.Position = startByte;
                int read = fs.Read(buffer, 0, endByte - startByte);
                fs.Flush();
                fs.Close();
                writer.AutoFlush = true;
                writer.WriteLine("HTTP/1.0 206 Partial Content");
                writer.WriteLine("Content-Type: video/mp4");
                writer.WriteLine("Accept-Ranges: bytes");
                int totalCount = startByte + buffer.Length;
                writer.WriteLine(string.Format("Content-Range: bytes {0}-{1}/{2}", startByte, totalCount - 1, totalCount));
                writer.WriteLine("Content-Length: " + buffer.Length.ToString());
                writer.WriteLine("Connection: keep-alive");
                writer.WriteLine("");
                writer.AutoFlush = false;

                writer.BaseStream.Write(buffer, 0, buffer.Length);
                writer.BaseStream.Flush();
            }



        }

        public static byte[] ImageToByte(Image img)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }

        private static void WriteResponse(StreamWriter writer, string contentType, byte[] content)
        {
            LogMessage("Writing response");
            writer.AutoFlush = true;
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

            writer.BaseStream.Write(content, 0, content.Length);
        }

        private static TcpRequest GetRequest(Stream stream)
        {
            TcpRequest req = new TcpRequest(stream);
            req.ReadRequest();
            return req;
        }

        private static void Write(Stream stream, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }
        // public override void handleGETRequest(HttpProcessor p)
        // {
        //     LogMessage("request: {0}", p.http_url);
        //     p.writeSuccess();
        //     writer.WriteLine("<html><body><h1>test server</h1>");
        //     writer.WriteLine("Current Time: " + DateTime.Now.ToString());
        //     writer.WriteLine("url : {0}", p.http_url);

        //     writer.WriteLine("<form method=post action=/form>");
        //     writer.WriteLine("<input type=text name=foo value=foovalue>");
        //     writer.WriteLine("<input type=submit name=bar value=barvalue>");
        //     writer.WriteLine("</form>");
        // }

        // public override void handlePOSTRequest(HttpProcessor p, StreamReader inputData)
        // {
        //     LogMessage("POST request: {0}", p.http_url);
        //     string data = inputData.ReadToEnd();

        //     writer.WriteLine("<html><body><h1>test server</h1>");
        //     writer.WriteLine("<a href=/test>return</a><p>");
        //     writer.WriteLine("postbody: <pre>{0}</pre>", data);
        // }

        private static void LogMessage(string msg)
        {
            //
            Console.WriteLine(msg);
        }
    }
}