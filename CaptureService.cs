
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace TCPCameraStream
{
    public class CaptureService : ICaptureService
    {
        public static int ID = 0;
        public static int _completed = 0;
        public static CaptureService Instance { get; private set; }
        private readonly int _cameraIndex;
        private VideoCapture _capture; 
        private bool _running;  

        public EventHandler<byte[]> OnCapture {get;set;}
         
        public static CaptureService Create(int camIndex)
        {
            return Instance = new CaptureService(camIndex);
        }
        private CaptureService(int cameraIndex)
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
            while (_running)
            {
                Mat originalmat = _capture.QueryFrame();
                Mat mat = GetContentScaled(originalmat, 0.5, 0.5, 0, 0);
               
                Image<Bgr, Byte> img = mat.ToImage<Bgr, Byte>();
                OnCapture?.Invoke(this, img.ToJpegData());
               
            }
        }

        private Mat GetContentScaled(Mat src, double xScale, double yScale, double xTrans, double yTrans, Inter interpolation = Inter.Linear)
        {
            var dst = new Mat(src.Size, src.Depth, src.NumberOfChannels);
            var translateTransform = new Matrix<double>(2, 3)
            {
                [0, 0] = xScale, // xScale
                [1, 1] = yScale, // yScale
                [0, 2] = xTrans + (src.Width - src.Width * xScale) / 2.0, //x translation + compensation of  x scaling
                [1, 2] = yTrans + (src.Height - src.Height * yScale) / 2.0 // y translation + compensation of y scaling
            };
            CvInvoke.WarpAffine(src, dst, translateTransform, dst.Size, interpolation);

            return dst;
        }
        public void Stop()
        {
            _running = false;
            _capture?.Stop();
        }
    }
}