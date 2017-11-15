using System.Windows;
using Microsoft.Kinect;
using System.Diagnostics;

namespace ReIdentificator
{
   
    public partial class MainWindow : Window
    {
        private KinectSensor kinect = null;
        private BodyDetector bodyDetector;
        private Comparer comparer;
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
    }
}
