﻿using System.Windows;
using Microsoft.Kinect;
using System;
using System.Windows.Media.Imaging;

namespace ReIdentificator
{
    public partial class MainWindow : Window
    {
        private KinectSensor kinect = null;
        private BodyProcessor bodyProcessor;
        private ShapeProcessor shapeProcessor;
        private ImageProcessor imageProcessor;
        private Comparer comparer;
        private MultiSourceFrameReader multiSourceFrameReader = null;
        private WriteableBitmap bitmap = null;
        private int skipFrameTicker = 0;

        public event EventHandler<LeftViewEventArgs> BodyLeftView;

        public MainWindow()
        {

            InitializeComponent();
            this.kinect = KinectSensor.GetDefault();
            this.kinect.Open();
            this.comparer = new Comparer();
            this.bodyProcessor = new BodyProcessor(this, this.kinect, this.comparer);
            this.shapeProcessor = new ShapeProcessor(this, this.kinect, this.comparer);
            this.imageProcessor = new ImageProcessor(this.kinect, this.comparer, this);

            
            this.bitmap = new WriteableBitmap(kinect.DepthFrameSource.FrameDescription.Width,
            kinect.DepthFrameSource.FrameDescription.Height, 96, 96, System.Windows.Media.PixelFormats.Gray8, null);
            //FrameDisplayImage.Source = bitmap;
            this.multiSourceFrameReader =
            this.kinect.OpenMultiSourceFrameReader(
             FrameSourceTypes.Body | /*FrameSourceTypes.Color |*/ FrameSourceTypes.BodyIndex | FrameSourceTypes.Depth);
            this.multiSourceFrameReader.MultiSourceFrameArrived +=
            this.Reader_MultiSourceFrameArrived;

        }
        public void RenderPixelArray(byte[] pixels)
        {
           int stride = bitmap.PixelWidth;
           bitmap.WritePixels(
               new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
               pixels, stride, 0);

           FrameDisplayImage.Source = bitmap;
        }
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            skipFrameTicker++;
            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

            if (multiSourceFrame == null)
            {
                return;
            }

            using (DepthFrame depthFrame =
            multiSourceFrame.DepthFrameReference.AcquireFrame())
            using (BodyIndexFrame bodyIndexFrame =
            multiSourceFrame.BodyIndexFrameReference.AcquireFrame())
            using (ColorFrame colorFrame =
            multiSourceFrame.ColorFrameReference.AcquireFrame())
            using (BodyFrame bodyFrame =
            multiSourceFrame.BodyFrameReference.AcquireFrame())
            {
                try
                {
                    if (bodyFrame != null)
                    {
                        bodyProcessor.processBodyFrame(bodyFrame);
                    }
                    if (bodyFrame != null && colorFrame != null)
                    {
                        //processImage
                    }
                    if (bodyIndexFrame != null && depthFrame != null && bodyFrame != null && skipFrameTicker % 15 == 0)
                    {
                       shapeProcessor.processBodyIndexFrame(bodyIndexFrame, depthFrame, bodyFrame);
                    }
                }
                finally
                {
                    if (bodyFrame != null)
                    {
                        bodyFrame.Dispose();
                    }
                    if (colorFrame != null)
                    {
                        colorFrame.Dispose();
                    }
                    if (bodyIndexFrame != null)
                    {
                        bodyIndexFrame.Dispose();
                    }

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
