using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ReIdentificator
{
    class ImageProcessor
    {
        private int errorSpan;
        private static JointType[] jointTypesToTrack = { JointType.ShoulderLeft, JointType.ShoulderRight };
        private KinectSensor kinect;
        private Comparer comparer;
        private ColorFrameReader colorFrameReader = null;
        private byte[] colorPixels;
        private WriteableBitmap colorBitmap;
        private MainWindow mainWindow;
        private Dictionary<ulong, List<byte[]>[]> colors = new Dictionary<ulong, List<byte[]>[]>();

        public ImageProcessor(KinectSensor kinect, Comparer comparer, MainWindow mainWindow)
        {
            this.kinect = kinect;
            this.comparer = comparer;
            this.mainWindow = mainWindow;

            this.colorFrameReader = this.kinect.ColorFrameSource.OpenReader();
            this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;

            mainWindow.BodyLeftView += HandleBodyLeftViewEvent;
        }

        public byte[] getColorOfJoint(Joint joint)
        {
            ColorSpacePoint colorPoint = kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);
            Debug.WriteLine("screen coords: " + colorPoint.X + " , " + colorPoint.Y);

            // calculate pixel index of joint coordinate
            int pixelIndex = (int)Math.Floor(colorPoint.Y);
            pixelIndex *= 1920;
            pixelIndex += (int)Math.Floor(colorPoint.X);
            pixelIndex *= 4;

            // extract color components
            byte red = colorPixels[pixelIndex + 2];
            byte green = colorPixels[pixelIndex + 1];
            byte blue = colorPixels[pixelIndex];
            byte opacity = colorPixels[pixelIndex + 3];

            // log
            Debug.WriteLine("color: " + red + ", " + green + ", " + blue + ", " + opacity);

            byte[] asarray = { red, green, blue, opacity };
            return asarray;
        }

        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    this.colorPixels = new byte[this.kinect.ColorFrameSource.FrameDescription.LengthInPixels * 4];
                    colorFrame.CopyConvertedFrameDataToArray(this.colorPixels, Microsoft.Kinect.ColorImageFormat.Bgra);
                    this.colorBitmap = new WriteableBitmap(this.kinect.ColorFrameSource.FrameDescription.Width, this.kinect.ColorFrameSource.FrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

                    // Write the pixel data into our bitmap
                    this.colorBitmap.WritePixels(
                        new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                        this.colorPixels,
                        this.colorBitmap.PixelWidth * sizeof(int),
                        0);

                    //mainWindow.FrameDisplayImage.Source = this.colorBitmap;

                    Body[] bodies = this.mainWindow.getBodyProcessor().getBodies();
                    if (bodies != null)
                    {
                        for (int bodyIndex = 0; bodyIndex < bodies.Length; bodyIndex++)
                        {
                            if(bodies[bodyIndex] != null && bodies[bodyIndex].IsTracked)
                            {
                                Joint shoulderLeft = bodies[bodyIndex].Joints[JointType.ShoulderLeft];
                                byte[] shoulderLeftColor = this.getColorOfJoint(shoulderLeft);

                                Joint shoulderRight = bodies[bodyIndex].Joints[JointType.ShoulderRight];
                                byte[] shoulderRightColor = this.getColorOfJoint(shoulderRight);

                                // output to user
                                switch (bodyIndex)
                                {
                                    case 0:
                                        mainWindow.ColorBoxLeftShoulder1.Background = new SolidColorBrush(Color.FromRgb(shoulderLeftColor[0], shoulderLeftColor[1], shoulderLeftColor[2]));
                                        mainWindow.ColorBoxRightShoulder1.Background = new SolidColorBrush(Color.FromRgb(shoulderRightColor[0], shoulderRightColor[1], shoulderRightColor[2]));
                                        break;
                                    case 1:
                                        mainWindow.ColorBoxLeftShoulder2.Background = new SolidColorBrush(Color.FromRgb(shoulderLeftColor[0], shoulderLeftColor[1], shoulderLeftColor[2]));
                                        mainWindow.ColorBoxRightShoulder2.Background = new SolidColorBrush(Color.FromRgb(shoulderRightColor[0], shoulderRightColor[1], shoulderRightColor[2]));
                                        break;
                                    case 2:
                                        mainWindow.ColorBoxLeftShoulder3.Background = new SolidColorBrush(Color.FromRgb(shoulderLeftColor[0], shoulderLeftColor[1], shoulderLeftColor[2]));
                                        mainWindow.ColorBoxRightShoulder3.Background = new SolidColorBrush(Color.FromRgb(shoulderRightColor[0], shoulderRightColor[1], shoulderRightColor[2]));
                                        break;
                                    case 3:
                                        mainWindow.ColorBoxLeftShoulder4.Background = new SolidColorBrush(Color.FromRgb(shoulderLeftColor[0], shoulderLeftColor[1], shoulderLeftColor[2]));
                                        mainWindow.ColorBoxRightShoulder4.Background = new SolidColorBrush(Color.FromRgb(shoulderRightColor[0], shoulderRightColor[1], shoulderRightColor[2]));
                                        break;
                                    case 4:
                                        mainWindow.ColorBoxLeftShoulder5.Background = new SolidColorBrush(Color.FromRgb(shoulderLeftColor[0], shoulderLeftColor[1], shoulderLeftColor[2]));
                                        mainWindow.ColorBoxRightShoulder5.Background = new SolidColorBrush(Color.FromRgb(shoulderRightColor[0], shoulderRightColor[1], shoulderRightColor[2]));
                                        break;
                                    case 5:
                                        mainWindow.ColorBoxLeftShoulder6.Background = new SolidColorBrush(Color.FromRgb(shoulderLeftColor[0], shoulderLeftColor[1], shoulderLeftColor[2]));
                                        mainWindow.ColorBoxRightShoulder6.Background = new SolidColorBrush(Color.FromRgb(shoulderRightColor[0], shoulderRightColor[1], shoulderRightColor[2]));
                                        break;
                                }

                                // save in this body's color timeseries
                                if (!this.colors.ContainsKey(bodies[bodyIndex].TrackingId))
                                {
                                    // if necessary instantiate list
                                    // for now, include 2 joints: shoulderLeft and shoulderRight
                                    this.colors[bodies[bodyIndex].TrackingId] = new List<byte[]>[2];
                                    this.colors[bodies[bodyIndex].TrackingId][0] = new List<byte[]>();
                                    this.colors[bodies[bodyIndex].TrackingId][1] = new List<byte[]>();
                                    //[2];
                                }
                                // save it
                                this.colors[bodies[bodyIndex].TrackingId][0].Add(shoulderLeftColor);
                                this.colors[bodies[bodyIndex].TrackingId][1].Add(shoulderRightColor);
                            }
                        }

                        if(bodies[0] == null || (bodies[0] != null && !bodies[0].IsTracked))
                        {
                            this.mainWindow.ColorBoxLeftShoulder1.Background = new SolidColorBrush(Colors.LightGray);
                            this.mainWindow.ColorBoxRightShoulder1.Background = new SolidColorBrush(Colors.LightGray);
                        }
                    }
                }
            }
        }

        byte[] averageColor(List<byte[]> colors)
        {
            int red = 0;
            int green = 0;
            int blue = 0;
            int opacity = 0;
            int length = 0;

            colors.ForEach(color => {
                red += (int)Math.Pow(color[0], 2);
                green += (int)Math.Pow(color[1], 2);
                blue += (int)Math.Pow(color[2], 2);
                opacity += (int)Math.Pow(color[3], 2);
                length++;
            });

            byte[] asarray = {
                (byte)Math.Sqrt(red / length),
                (byte)Math.Sqrt(green / length),
                (byte)Math.Sqrt(blue / length),
                (byte)Math.Sqrt(opacity / length)
            };

            //needs to be tested
            //int[] indicator = IndicatorColor(asarray);
            //asarray = PostAvgColor(colors, indicator, errorSpan);

            return asarray;
        }

        void HandleBodyLeftViewEvent(object sender, LeftViewEventArgs e)
        {
            Console.WriteLine("body with id " + e.TrackingId + " has left frame");
            byte[] avgcolLeftShoulder = averageColor(this.colors[e.TrackingId][0]);
            byte[] avgcolRightShoulder = averageColor(this.colors[e.TrackingId][1]);
            mainWindow.printLog("average color of left shoulder of person with id " + e.TrackingId + ": " + avgcolLeftShoulder[0] + ", " + avgcolLeftShoulder[1] + ", " + avgcolLeftShoulder[2] + ", " + avgcolLeftShoulder[3]);
            mainWindow.printLog("average color of right shoulder of person with id " + e.TrackingId + ": " + avgcolRightShoulder[0] + ", " + avgcolRightShoulder[1] + ", " + avgcolRightShoulder[2] + ", " + avgcolRightShoulder[3]);
        }

        int[] IndicatorColor(byte[] asarray)
         {
           int indicator = 0;
           int red = (int)Math.Pow(asarray[0], 2);
           int green = (int)Math.Pow(asarray[1], 2);
           int blue = (int)Math.Pow(asarray[2], 2);
            indicator = red + blue + green;
            int[] indicatorColor = { red, green, blue, indicator };
           //opacity += (int)Math.Pow(color[3], 2);
           return indicator;
         }

         byte[] PostAvgColor(List<byte[]> colors, int[] preColors, byte intError)
         {
            //195075 == (255^2)*3
             byte error = intError;
             int overallError = (int) (error*195075);
             int eachError = (int)(error * 65025);

             int red = 0;
             int green = 0;
             int blue = 0;
             //int opacity = 0;
             int length = 0;

             colors.ForEach(color => {

                int redTemp = 0;
                int greenTemp = 0;
                int blueTemp = 0;
                //int opacityTemp = 0;
                 redTemp += (int)Math.Pow(color[0], 2);
                 greenTemp += (int)Math.Pow(color[1], 2);
                 blueTemp += (int)Math.Pow(color[2], 2);
                 //opacityTemp += (int)Math.Pow(color[3], 2);
                int comparison = redTemp + greenTemp + blueTemp; //+opacityTemp
                 if (comparison > preColors[3] - error && comparison < preColors[3] + error)
                 {
                     if (redTemp > preColors[0] - error * 1.5 && redTemp < preColors[0] + error * 1.5)
                     {
                         if (greenTemp > preColors[1] - error * 1.5 && greenTemp < preColors[1] + error * 1.5)
                         {
                             if (blueTemp > preColors[2] - error * 1.5 && blueTemp < preColors[2] + error * 1.5)
                             {
                                 red += redTemp;
                                 green += greenTemp;
                                 blue += blueTemp;
                                 length++;
                             }
                         }
                     }
                 }
             });

             byte[] asarray = {
                 (byte)Math.Sqrt(red / length),
                 (byte)Math.Sqrt(green / length),
                 (byte)Math.Sqrt(blue / length),
                 //(byte)Math.Sqrt(opacity / length)
                 255
             };

            //If less than three quarters of all Points are in the Span
            if(asarray.Length < colors.length*0.75)
            {
                asarry = PostAvgColor(colors, preColors, intError + 0.1);
            }

            return asarray;

             /*int error = 0;

             for (int i = 0; i < colors.Length; i++)
             {
                 indicatorsError[i] = IndicatorColor(colors[0.r], colors[0.g], colors[0.b], preColor.r, preColor.b, preColor.g);
                 error += indicatorsError[i];
             }
             error = error / indicatorsError.Length;
             for (int i = 0; i < indicatorsError.Length; i++)
             {
                 if(indicatorsError[i] < error)
                 {
                     j++;
                     postErrorColors[j] = colors[i];
                 }
             }

             Color finalColor = postErrorColors[0];
             for (int i = 1; i < postErrorColors.Length; i ++)
             {
                 arrCol = postErrorColors[i]
                 finalColor = AvgColor(arrCol.r, arrCol.g, arrCol.b, finalColor.r, finalColor.g, finalColor.b);
             }
             return finalColor;*/
         }
    }
}
