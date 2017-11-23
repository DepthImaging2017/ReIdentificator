using System.Windows;
using Microsoft.Kinect;
using System;

namespace ReIdentificator
{
   
    public partial class MainWindow : Window
    {
        private KinectSensor kinect = null;
        private BodyDetector bodyDetector;
        private ImageDetector imageDetector;
        private Comparer comparer;
        public event EventHandler<LeftViewEventArgs> BodyLeftView;

        public MainWindow()
        {
            this.kinect = KinectSensor.GetDefault();
            this.kinect.Open();
            this.comparer = new Comparer();
            this.bodyDetector = new BodyDetector(this, this.kinect, this.comparer);
            this.imageDetector = new ImageDetector(this.kinect, this.comparer, this);
            InitializeComponent();
        }
        public void printLog(string logtext)
        {
            LoggingBox.AppendText("\n"+ logtext);
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
