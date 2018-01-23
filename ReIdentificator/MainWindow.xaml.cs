using System.Windows;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Kinect;
using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Media;
using System.Reflection;

namespace ReIdentificator
{
    public partial class MainWindow : Window
    {
        private KinectSensor kinect = null;
        private BodyProcessor bodyProcessor;
        private ShapeProcessor shapeProcessor;
        private ImageProcessor imageProcessor;
        private FaceAPI faceAPI;

        private Comparer comparer;
        private MultiSourceFrameReader multiSourceFrameReader = null;
        private Database db;
        private WriteableBitmap bitmap = null;
        private int skipFrameTicker = 0;
        private List<dataForComparison> dataForComparison_list = new List<dataForComparison>();

        public event EventHandler<LeftViewEventArgs> BodyLeftView;

        public MainWindow()
        {
            InitializeComponent();
            this.kinect = KinectSensor.GetDefault();
            this.kinect.Open();
            this.db = new Database("mongodb://localhost:27017", "depthImaging", "individuals");
            this.comparer = new Comparer(this.db, this);
            this.bodyProcessor = new BodyProcessor(this, this.kinect, this.comparer);
            this.shapeProcessor = new ShapeProcessor(this, this.kinect, this.comparer);
            this.imageProcessor = new ImageProcessor(this.kinect, this.comparer, this);
            this.faceAPI = new FaceAPI(this.kinect, this.comparer, this);

            this.bitmap = new WriteableBitmap(kinect.DepthFrameSource.FrameDescription.Width,
            kinect.DepthFrameSource.FrameDescription.Height, 96, 96, System.Windows.Media.PixelFormats.Gray8, null);
            //FrameDisplayImage.Source = bitmap;
            this.multiSourceFrameReader =
            this.kinect.OpenMultiSourceFrameReader(
             FrameSourceTypes.Body | FrameSourceTypes.Color | FrameSourceTypes.BodyIndex | FrameSourceTypes.Depth);
            this.multiSourceFrameReader.MultiSourceFrameArrived +=
            this.Reader_MultiSourceFrameArrived;
        }
        public void startComparison(ulong trackingId, object data)
        {
            int numberOfProcessors = 3;

            if (!dataForComparison_list.Exists(element => element.TrackingId == trackingId))
            {
                dataForComparison_list.Add(new dataForComparison(trackingId));
            }
            dataForComparison currentComparisonData = dataForComparison_list.Find(element => element.TrackingId == trackingId);
            currentComparisonData.processorData.Add(data);
            if (currentComparisonData.processorData.Count == numberOfProcessors)
            {
                Individual idv = new Individual();
                for (int i = 0; i < currentComparisonData.processorData.Count; i++)
                {
                    PropertyInfo[] properties = currentComparisonData.processorData[i].GetType().GetProperties();
                    foreach (PropertyInfo pi in properties)
                    {
                        if (idv.GetType().GetProperty(pi.Name) != null)
                        {
                            Debug.WriteLine("Hey");
                            idv.GetType().GetProperty(pi.Name).SetValue(idv, pi.GetValue(currentComparisonData.processorData[i], null));
                        }
                    }

                }
                db.GetAllEntries((result) =>
                {
                    comparer.compare(idv, result);
                    //bool reÌdentified = comparer.compare(idv, result);
                    //if (!reÌdentified)
                    //{
                    //    printLog("Person that left the frame is not reidentified");
                    //    db.AddEntry(idv, null);
                    //}
                    //else
                    //{
                    //    printLog("Person that left the frame is reidentified!");
                    //}
                    dataForComparison_list.Remove(currentComparisonData);

                });

            }

        }

        public BodyProcessor getBodyProcessor()
        {
            return this.bodyProcessor;
        }

        public void RenderPixelArray(byte[] pixels, Image targetImage)
        {
           int stride = bitmap.PixelWidth;
           bitmap.WritePixels(
               new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
               pixels, stride, 0);

            targetImage.Source = bitmap;
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
                        imageProcessor.processColorFrame(colorFrame, bodyFrame);
                    }
                    if (bodyIndexFrame != null && depthFrame != null && bodyFrame != null && skipFrameTicker % 15 == 0)
                    {
                       shapeProcessor.processBodyIndexFrame(bodyIndexFrame, depthFrame, bodyFrame);
                       faceAPI.nui_ColorFrameReady(colorFrame, bodyFrame);

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
        public void StackPanelInit(int numberOfJoints)
        {
            Thickness myMargin = new Thickness();
            myMargin.Bottom = 5;
            myMargin.Left = 0;
            myMargin.Right = 5;
            myMargin.Top = 0;
            StackPanel[] stackPanelArray = new StackPanel[6];
            for(int i = 0; i < stackPanelArray.Length; i++)
            {
                TextBlock number = new TextBlock();
                number.Text = ""+(i+1);
                StackPanel myStackPanel = new StackPanel();
                stackPanelArray[i] = myStackPanel;
                myStackPanel.Orientation = Orientation.Horizontal;
                myStackPanel.Name = "StackPanel" + i;
                stackPanelArray[i].Children.Add(number);
                for (int j = 0; j < numberOfJoints; j++)
                {
                    Canvas colorField = new Canvas();
                    colorField.Height = 80;
                    colorField.Width = 80;
                    colorField.Background = Brushes.LightGray;
                    colorField.Margin = myMargin;
                    colorField.Name = "JointNumber" + j;
                    stackPanelArray[i].Children.Add(colorField);
                }
                userOutput.Children.Add(stackPanelArray[i]);
            }
        }
        public void updatePanel(byte[,] colors, double fieldToShow)
        {
            userOutput.Children.RemoveAt((int)fieldToShow);
            Thickness myMargin = new Thickness();
            myMargin.Bottom = 5;
            myMargin.Left = 0;
            myMargin.Right = 5;
            myMargin.Top = 0;
            StackPanel stackPanel = new StackPanel();
            TextBlock number = new TextBlock();
            number.Text = "" + (fieldToShow+1);
            StackPanel myStackPanel = new StackPanel();
            myStackPanel.Orientation = Orientation.Horizontal;
            myStackPanel.Name = "StackPanel" + fieldToShow;
            myStackPanel.Children.Add(number);
            for (int j = 0; j < colors.GetLength(0); j++)
            {
                Canvas colorField = new Canvas();
                colorField.Height = 80;
                colorField.Width = 80;
                colorField.Background = new SolidColorBrush(Color.FromRgb(colors[j, 0], colors[j, 1], colors[j, 2]));
                colorField.Margin = myMargin;
                colorField.Name = "JointNumber" + j;
                myStackPanel.Children.Add(colorField);
            }
            userOutput.Children.Insert((int)fieldToShow, myStackPanel);
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
    public class dataForComparison
    {
        public ulong TrackingId { get; set; }
        public List<object> processorData { get; set; } = new List<object>();
        public dataForComparison(ulong trackingId)
        {
            this.TrackingId = trackingId;
        }
    }
}
