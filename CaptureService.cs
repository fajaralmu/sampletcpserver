
using System;
using System.Threading.Tasks;
using Emgu.CV;

namespace TCPCameraStream
{
    public class CaptureService
    {
        private readonly int _cameraIndex;
        private VideoCapture _capture;
        private bool _running;
        public CaptureService(int cameraIndex)
        {
            _cameraIndex = cameraIndex;
            
        }

        public void Start()
        {
            _capture = new VideoCapture(_cameraIndex)
            {
                FlipHorizontal = true
            };
            _running = true;
            Task.Run(Run);
        }

        private void Run()
        {
            while ( _running )
            {
                Mat img = _capture.QueryFrame();
            }
        }

        public void Stop()
        {
            _running = false;
            _capture?.Stop();
        }
    }
}