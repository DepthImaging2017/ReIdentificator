using System.Windows;
using Microsoft.Kinect;
using System;

namespace ReIdentificator
{
   
    public partial class MainWindow : Window
    {
        private KinectSensor kinect = null;
        private BodyDetector bodyDetector;
        private Comparer comparer;
        public event EventHandler<LeftFrameEventArgs> BodyLeftFrame;

        public MainWindow()
        {
            this.kinect = KinectSensor.GetDefault();
            this.kinect.Open();
            this.comparer = new Comparer();
            this.bodyDetector = new BodyDetector(this, this.kinect, this.comparer);
            InitializeComponent();
        }
        public void printLog(string logtext)
        {
            LoggingBox.AppendText("\n"+ logtext);
        }
        public void raisePersonLeftFrameEvent(ulong trackingId)
        {
            OnBodyLeftFrame(new LeftFrameEventArgs(trackingId));
        }
        protected virtual void OnBodyLeftFrame(LeftFrameEventArgs e)
        {
            BodyLeftFrame?.Invoke(this, e);
        }
    }
    public class LeftFrameEventArgs : EventArgs
    {
        private ulong trackingId;
        public LeftFrameEventArgs(ulong id)
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
