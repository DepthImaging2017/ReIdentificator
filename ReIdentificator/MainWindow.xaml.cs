using System.Windows;
using Microsoft.Kinect;
using System;

namespace ReIdentificator
{
    public partial class MainWindow : Window
    {
        private KinectSensor kinect = null;
        private BodyProcessor bodyProcessor;
        private ImageProcessor imageProcessor;
        private Comparer comparer;
        private MultiSourceFrameReader multiSourceFrameReader = null;

        public event EventHandler<LeftViewEventArgs> BodyLeftView;

        public MainWindow()
        {
            InitializeComponent();
            this.kinect = KinectSensor.GetDefault();
            this.kinect.Open();
            this.comparer = new Comparer();
            this.bodyProcessor = new BodyProcessor(this, this.kinect, this.comparer);
            this.imageProcessor = new ImageProcessor(this.kinect, this.comparer, this);
            this.multiSourceFrameReader =
            this.kinect.OpenMultiSourceFrameReader(
             FrameSourceTypes.Body | FrameSourceTypes.Color);
            this.multiSourceFrameReader.MultiSourceFrameArrived +=
            this.Reader_MultiSourceFrameArrived;
        }        
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

            if (multiSourceFrame == null)
            {
                return;
            }

            using (ColorFrame colorFrame =
            multiSourceFrame.ColorFrameReference.AcquireFrame())
            using (BodyFrame bodyFrame =
            multiSourceFrame.BodyFrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    bodyProcessor.processBodyFrame(bodyFrame);
                }
                if (bodyFrame != null && colorFrame != null)
                {
                   //processImage
                }
                
            } 

        }
        public void printLog(string logtext)
        {
            LoggingBox.AppendText("\n" + logtext);
        }
        public void raisePersonLeftViewEvent(ulong trackingId)
        {
            OnBodyLeftView(new LeftViewEventArgs(trackingId));
        }
        protected virtual void OnBodyLeftView(LeftViewEventArgs e)
        {
            BodyLeftView?.Invoke(this, e);
        }
    }
    public class LeftViewEventArgs : EventArgs
    {
        private ulong trackingId;
        public LeftViewEventArgs(ulong id)
        {
            trackingId = id;
        }

        public ulong TrackingId
        {
            get { return trackingId; }
            set { trackingId = value; }
        }
    }
}
