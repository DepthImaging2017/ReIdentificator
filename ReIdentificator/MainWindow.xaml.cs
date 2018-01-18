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
using System.Windows.Input;
using System.Windows.Forms;
using DexterLib;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

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
            this.faceAPI = new FaceAPI(this.kinect, this.comparer, this);

            this.bitmap = new WriteableBitmap(kinect.DepthFrameSource.FrameDescription.Width,
            kinect.DepthFrameSource.FrameDescription.Height, 96, 96, System.Windows.Media.PixelFormats.Gray8, null);
            FrameDisplayImage.Source = bitmap;
            this.multiSourceFrameReader =
            this.kinect.OpenMultiSourceFrameReader(
             FrameSourceTypes.Body | FrameSourceTypes.Color | FrameSourceTypes.BodyIndex | FrameSourceTypes.Depth);
            this.multiSourceFrameReader.MultiSourceFrameArrived +=
            this.Reader_MultiSourceFrameArrived;

            this.db = new Database("mongodb://localhost:27017", "depthImaging", "individuals");
        }

        public BodyProcessor getBodyProcessor()
        {
            return this.bodyProcessor;
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

                /* BitmapEncoder encoder = new JpegBitmapEncoder();

                 encoder.Frames.Add(BitmapFrame.Create(bitmap));
                 String path = 'C:\Users' + '\Benjamin Karic' + '\new';
                 try
                 {
                     using (FileStream fs = new FileStream(path, FileMode.Create))
                     {
                         encoder.Save(fs);
                     }
                 }
                 catch (IOException)
                 {
                     System.Windows.MessageBox.Show("Save Failed");
                 }*/
                try
                {


                    if (bodyFrame != null)
                    {
                        bodyProcessor.processBodyFrame(bodyFrame);
                    }
                    if (bodyFrame != null && colorFrame != null && skipFrameTicker % 150 == 0)
                    {
                        imageProcessor.processColorFrame(colorFrame, bodyFrame);
                        faceAPI.nui_ColorFrameReady(colorFrame, bodyFrame);

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



        public void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            faceAPI.BrowseButton_Click1(sender, e);
        }

        public void FacePhoto_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            faceAPI.FacePhoto_MouseMove1(sender, e);
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
                myStackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
                myStackPanel.Name = "StackPanel" + i;
                stackPanelArray[i].Children.Add(number);
                for (int j = 0; j < numberOfJoints; j++)
                {
                    Canvas colorField = new Canvas();
                    colorField.Height = 80;
                    colorField.Width = 80;
                    colorField.Background = System.Windows.Media.Brushes.LightGray;
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
            myStackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
            myStackPanel.Name = "StackPanel" + fieldToShow;
            myStackPanel.Children.Add(number);
            for (int j = 0; j < colors.GetLength(0); j++)
            {
                Canvas colorField = new Canvas();
                colorField.Height = 80;
                colorField.Width = 80;
                colorField.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(colors[j, 0], colors[j, 1], colors[j, 2]));
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
}

/*public class Form1 : System.Windows.Forms.Form
{
    string fileName;           //used to save the movie file name 
    string storagePath;        //used for the path where we save files
    MediaDetClass md;          //needed to extract pictures
    static int counter = 0;    //to generate different file names
    float interval = 1.0f;     //default time interval

    
    private System.Windows.Forms.MainMenu File;
    private System.Windows.Forms.MenuItem miFile;
    private System.Windows.Forms.MenuItem miOpenFile;
    private System.Windows.Forms.Label label1;
    
    private System.Windows.Forms.Button SaveButton;
    private System.Windows.Forms.Button ScanButton;
    private System.Windows.Forms.MenuItem menuItem1;
    private System.Windows.Forms.Button backward;
    private System.Windows.Forms.Button forward;
    private System.Windows.Forms.MenuItem miSpeed;
    private System.Windows.Forms.MenuItem miNormalSpeed;
    private System.Windows.Forms.MenuItem miFastSpeed;
    private System.Windows.Forms.PictureBox pictureBox1;
    private System.Windows.Forms.TrackBar trackBar1;
    private System.ComponentModel.Container components = null;

    public Form1()
    {
        //InitializeComponent();

        //initialize a few properties
        trackBar1.Minimum = trackBar1.Maximum = 0;
        this.MaximizeBox = false;
        miNormalSpeed.Checked = true;
        storagePath = System.Windows.Forms.Application.StartupPath + "\\tmp\\";

        //if the storage directory doesn't exist we create it
        if (!Directory.Exists(storagePath))
            Directory.CreateDirectory(storagePath);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (components != null)
            {
                components.Dispose();
            }
        }
        try
        {
            //try to get rid of all the tmp files created during the session
            if (pictureBox1.Image != null)
                pictureBox1.Image.Dispose();
            string[] bmpFiles = Directory.GetFiles(storagePath, "*.bmp");
            for (int i = 0; i < bmpFiles.Length; i++)
                System.IO.File.Delete(bmpFiles[i]);
        }
        catch (Exception) { System.Windows.Forms.MessageBox.Show("Couldn't delete all temporary files"); }
        base.Dispose(disposing);
    }

    

    [STAThread]

    private void SaveButton_Click(object sender, System.EventArgs e)
    {
        counter++;
        pictureBox1.Image.Dispose();
        string fBitmapName = storagePath + Path.GetFileNameWithoutExtension(fileName)
          + "_" + counter.ToString();
        md.WriteBitmapBits(trackBar1.Value, 320, 240, fBitmapName + ".bmp");
        pictureBox1.Image = new Bitmap(fBitmapName + ".bmp");
        //save the picture as jpeg
        System.Drawing.Image img = System.Drawing.Image.FromFile(fBitmapName + ".bmp");
        img.Save(fBitmapName + ".jpg", ImageFormat.Jpeg);
        img.Dispose();
    }

    // Thread class to be able to display the progress
    // using the static label
    class ScanThread
    {
        DexterLib.MediaDetClass md;
        string fileName;
        string storagePath;
        float interval;
        public Thread t;
        public ScanThread(string s, string f, float ival)
        {
            storagePath = s;
            fileName = f;
            interval = ival;
            t = new Thread(new ThreadStart(this.Scan));
            t.Start();
        }
        void Scan()
        {
            md = new MediaDetClass();
            System.Drawing.Image img;
            md.Filename = fileName;
            md.CurrentStream = 0;
            int len = (int)md.StreamLength;
            for (float i = 0.0f; i < len; i = i + interval)
            {
                counter++;
                string fBitmapName = storagePath + Path.GetFileNameWithoutExtension(fileName)
                  + "_" + counter.ToString();
                md.WriteBitmapBits(i, 320, 240, fBitmapName + ".bmp");
                img = System.Drawing.Image.FromFile(fBitmapName + ".bmp");
                img.Save(fBitmapName + ".jpg", ImageFormat.Jpeg);
                img.Dispose();
                System.IO.File.Delete(fBitmapName + ".bmp");
            }
        }
    }


    private void ScanButton_Click(object sender, System.EventArgs e)
    {
        if (md == null) return;

        ScanThread st = new ScanThread(storagePath, fileName, interval);
        do
        {
            //waits until the processing is done, displaying the
            //number of the file we are currently saving
            Thread.Sleep(1000);
            label1.Text = "Saving: " + counter.ToString();
            label1.Invalidate();
            label1.Update();
        } while (st.t.IsAlive);
        label1.Text = "Saving: DONE";
    }

    private void BrowseButton_Click1(object sender, EventArgs e)
    {
        OpenFileDialog dlg = new OpenFileDialog();
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            try
            {
                fileName = dlg.FileName;
                this.Text = Path.GetFileName(dlg.FileName);

                //create the MediaDetClass and set its properties
                md = new MediaDetClass();
                md.Filename = fileName;
                md.CurrentStream = 0;
                int len = (int)md.StreamLength;

                //fix a few Gui stuff
                label1.Text = "Length: " + len.ToString();
                trackBar1.Minimum = 0;
                trackBar1.Maximum = len;
                trackBar1.Value = 0;

                //make sure we have a unique name then call WriteBitmapBits to
                //a file then use it to fill the picture box
                counter++;
                string fBitmapName = storagePath + counter.ToString() + ".bmp";
                md.WriteBitmapBits(0, 320, 240, fBitmapName);
                pictureBox1.Image = new Bitmap(fBitmapName);
            }
            catch (Exception) { System.Windows.Forms.MessageBox.Show("Coulnd't open movie file"); }
        }
    }

    /*private void trackBar1_ValueChanged(object sender, System.EventArgs e)
    {
        if (md == null) return;
        pictureBox1.Image.Dispose();
        label1.Text = "Cur Pos: " + trackBar1.Value.ToString();
        string fBitmapName = storagePath + "tmp" + counter.ToString() + ".bmp";
        counter++;
        md.WriteBitmapBits(trackBar1.Value, 320, 240, fBitmapName);
        pictureBox1.Image = new Bitmap(fBitmapName);

    }

    private void backward_Click(object sender, System.EventArgs e)
    {
        if (trackBar1.Value >= trackBar1.Minimum + 1)
            trackBar1.Value = trackBar1.Value - 1;
    }

    private void forward_Click(object sender, System.EventArgs e)
    {
        if (trackBar1.Value <= trackBar1.Maximum - 1)
            trackBar1.Value = trackBar1.Value + 1;
    }

    private void miNormalSpeed_Click(object sender, System.EventArgs e)
    {
        miNormalSpeed.Checked = true;
        miFastSpeed.Checked = false;
        interval = 1.0f;
    }

    private void miFastSpeed_Click(object sender, System.EventArgs e)
    {
        miNormalSpeed.Checked = false;
        miFastSpeed.Checked = true;
        interval = 0.1f;

    }
}*/ 

