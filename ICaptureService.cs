using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPCameraStream
{
    public interface ICaptureService
    {
        public EventHandler<byte[]> OnCapture { get; set; }
        public void Start();
        public void Stop();
    }
}
