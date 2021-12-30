
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;

namespace TCPCameraStream
{
    public class CaptureService
    {
        public static int ID = 0;
        public static int _completed = 0;
        public static CaptureService Instance { get; private set; }
        private readonly int _cameraIndex;
        private VideoCapture _capture;
        private VideoWriter _writer;
        private bool _running;
        private bool _busy = false;
        private bool _writing = false;

        public EventHandler<byte[]> OnCapture {get;set;}
        public bool Busy
        {
            get => _writing || _busy; 
            set => _busy = value;
        }
        public static CaptureService Create(int camIndex)
        {
            return Instance = new CaptureService(camIndex);
        }
        private CaptureService(int cameraIndex)
        {
            _cameraIndex = cameraIndex;

        }

        public void StopWrite()
        {
            Console.WriteLine("Stop write video");
            _writing = false;
            _writer.Dispose();
            ID = _completed;
        }

        public void BeginWrite()
        {
            _completed++;
            
            Size size           = new System.Drawing.Size(300, 300);
            double totalFrames  = _capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameCount);
            double fps          = _capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Fps);
            int fourcc          = Convert.ToInt32(_capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FourCC));
            int frameHeight     = Convert.ToInt32(_capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameHeight));
            int frameWidth      = Convert.ToInt32(_capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameWidth));
            string destination  = $"output-{_completed}.mp4";
            
            _writer = new VideoWriter(destination, VideoWriter.Fourcc('m', 'p', '4', 'v'), fps, new Size(frameWidth, frameHeight), true);
            
            _writing = true;
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
            while (_running)
            {
                Mat mat = _capture.QueryFrame();
                if (_writing)
                {
                    try {
                        _writer.Write(mat);
                    } catch ( Exception e )
                    {

                    }
                }
                Image<Bgr, Byte> img = mat.ToImage<Bgr, Byte>();
                OnCapture?.Invoke(this, img.ToJpegData());
               
            }
        } 
        public void Stop()
        {
            _running = false;
            _capture?.Stop();
        }
    }
}