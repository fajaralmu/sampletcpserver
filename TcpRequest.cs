using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TCPCameraStream
{
    public class TcpRequest
    {
        private readonly Stream _stream;
        public string Path {get;set;}
        public string RequestInfo {get;set;}
        public IDictionary<string,string> Headers { get; } = new Dictionary<string, string>();
        private bool _stopRead = false;
        private long _time = 0;
        private string _data = "";
        private int _next_char = 0;
        public TcpRequest(Stream stream)
        {
            _stream = stream;
        }

        public string ReadRequest()
        {
            _time = Now();
            
            Task.Run(ReadBytes);
            Task.Run(TimeTrack);
            while (!_stopRead)
            {
                //
            }
            ExtractHeaders();
            return _data;
        }

        private void ExtractHeaders()
        {
            Headers.Clear();
            string[] raw    = _data.Split("\r\n");
            RequestInfo     = raw[0];
            Path            = raw[0].Split(" ")[1];
            
            for (int i = 1; i < raw.Length; i++)
            {
                string item = raw[i];
                string[] itemSplit = item.Split(": ");
                if (itemSplit.Length == 2)
                {
                    Headers.Add(itemSplit[0], itemSplit[1]);
                }
            }
        }

        private void TimeTrack()
        {
            while (true)
            {
                if (Now() - _time >= 500)
                {
                    _stopRead = true;
                    break;
                }
                Task.Delay(500).Wait();
            }
        }

        private void ReadBytes()
        {
            
            while (!_stopRead && _stream.CanRead)
            {
                _time = Now();

                try
                {
                    _next_char = _stream.ReadByte();
                }
                catch (Exception)
                {
                    _stopRead = true;
                    break;
                }
                if (_next_char < 0)
                {
                    _stopRead = true;
                    break;
                }
                _data += Convert.ToChar(_next_char);
            }
             
        }
        private void ReadBytes_old()
        {

            while (!_stopRead)
            {
                _time = Now();
                Console.WriteLine($"_stream.CanRead: {_stream.CanRead}");
                if (_stream.CanRead == false)
                {
                    _stopRead = true;
                    break;
                }
                try
                {
                    _next_char = _stream.ReadByte();
                }
                catch (Exception)
                {
                    _stopRead = true;
                    break;
                }
                if (_next_char < 0)
                {
                    _stopRead = true;
                    break;
                }
                _data += Convert.ToChar(_next_char);
            }

        }

        private long Now()
        {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }
    }
}