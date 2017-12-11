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
        private double deleteTheTopAndBottom = 0.05;
        private static JointType[] jointTypesToTrack = { JointType.ShoulderLeft, JointType.ShoulderRight };
        private int avgColorView = 0;
        private double[,] currColorToView = new double[6, 2] { {  - 1, -1 }, { -1, -1 }, { -1, -1 }, { -1, -1 }, { -1, -1 }, { -1, -1 } };
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
                        GetColorOfBodyParts(jointTypesToTrack, bodies);
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
            int[] indicator = IndicatorColor(asarray);
            asarray = PostAvgColor(colors, indicator, deleteTheTopAndBottom);

            return asarray;
        }

        void HandleBodyLeftViewEvent(object sender, LeftViewEventArgs e)
        {
            double fieldToShow = -1;
            Console.WriteLine("body with id " + e.TrackingId + " has left frame");
            byte[] avgColOfFirstJoint = averageColor(this.colors[e.TrackingId][0]);
            byte[] avgColOfSecJoint = averageColor(this.colors[e.TrackingId][1]);
            mainWindow.printLog("average color of the first joint of person with id " + e.TrackingId + ": " + avgColOfFirstJoint[0] + ", " + avgColOfFirstJoint[1] + ", " + avgColOfFirstJoint[2] + ", " + avgColOfFirstJoint[3]);
            mainWindow.printLog("average color of the second of person with id " + e.TrackingId + ": " + avgColOfSecJoint[0] + ", " + avgColOfSecJoint[1] + ", " + avgColOfSecJoint[2] + ", " + avgColOfSecJoint[3]);
            for (int i = 0; i < currColorToView.Length; i++)
            {
                if (currColorToView[i, 0] == e.TrackingId)
                {
                    fieldToShow = currColorToView[i, 1];
                    for(int j = 0; j > i; j--)
                    {
                        if(currColorToView[j, 0] != -1)
                        {
                            currColorToView[i, 1] = currColorToView[j, 1];
                            currColorToView[i, 0] = currColorToView[j, 0];
                            currColorToView[j, 0] = -1;
                            currColorToView[j, 1] = -1;
                        }
                    }
                    break;
                }
            }
            // output to user
            switch (fieldToShow)
            {
                case 0:
                            mainWindow.ColorBoxFirstJoint1.Background = new SolidColorBrush(Color.FromRgb(avgColOfFirstJoint[0], avgColOfFirstJoint[1], avgColOfFirstJoint[2]));
                            mainWindow.ColorBoxSecondJoint1.Background = new SolidColorBrush(Color.FromRgb(avgColOfSecJoint[0], avgColOfSecJoint[1], avgColOfSecJoint[2]));
                            break;
                case 1:
                            mainWindow.ColorBoxFirstJoint2.Background = new SolidColorBrush(Color.FromRgb(avgColOfFirstJoint[0], avgColOfFirstJoint[1], avgColOfFirstJoint[2]));
                            mainWindow.ColorBoxSecondJoint2.Background = new SolidColorBrush(Color.FromRgb(avgColOfSecJoint[0], avgColOfSecJoint[1], avgColOfSecJoint[2]));
                            break;
                case 2:

                            mainWindow.ColorBoxFirstJoint3.Background = new SolidColorBrush(Color.FromRgb(avgColOfFirstJoint[0], avgColOfFirstJoint[1], avgColOfFirstJoint[2]));
                            mainWindow.ColorBoxSecondJoint3.Background = new SolidColorBrush(Color.FromRgb(avgColOfSecJoint[0], avgColOfSecJoint[1], avgColOfSecJoint[2]));
                    break;
                case 3:
                            mainWindow.ColorBoxFirstJoint4.Background = new SolidColorBrush(Color.FromRgb(avgColOfFirstJoint[0], avgColOfFirstJoint[1], avgColOfFirstJoint[2]));
                            mainWindow.ColorBoxSecondJoint4.Background = new SolidColorBrush(Color.FromRgb(avgColOfSecJoint[0], avgColOfSecJoint[1], avgColOfSecJoint[2]));
                        break;
                case 4:
                            mainWindow.ColorBoxFirstJoint5.Background = new SolidColorBrush(Color.FromRgb(avgColOfFirstJoint[0], avgColOfFirstJoint[1], avgColOfFirstJoint[2]));
                            mainWindow.ColorBoxSecondJoint5.Background = new SolidColorBrush(Color.FromRgb(avgColOfSecJoint[0], avgColOfSecJoint[1], avgColOfSecJoint[2]));
                            break;
                case 5:
                            mainWindow.ColorBoxFirstJoint6.Background = new SolidColorBrush(Color.FromRgb(avgColOfFirstJoint[0], avgColOfFirstJoint[1], avgColOfFirstJoint[2]));
                            mainWindow.ColorBoxSecondJoint6.Background = new SolidColorBrush(Color.FromRgb(avgColOfSecJoint[0], avgColOfSecJoint[1], avgColOfSecJoint[2]));
                            break;
            }

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
           return indicatorColor;
         }

         void GetColorOfBodyParts(JointType[] jointTypesToTrack, Body[] bodies)
        {
           for (int bodyIndex = 0; bodyIndex < bodies.Length; bodyIndex++)
           {
             if(bodies[bodyIndex] != null && bodies[bodyIndex].IsTracked)
             {
               // save in this body's color timeseries
               if (!this.colors.ContainsKey(bodies[bodyIndex].TrackingId))
               {
                 this.colors[bodies[bodyIndex].TrackingId] = new List<byte[]>[jointTypesToTrack.Length];
                 for(int i = 0; i<jointTypesToTrack.Length; i++){
                   this.colors[bodies[bodyIndex].TrackingId][i] = new List<byte[]>();
                 }
               }

               for(int j = 0; j<jointTypesToTrack.Length; j++){
                 Joint bodyPart = bodies[bodyIndex].Joints[jointTypesToTrack[j]];
                 byte[] colorOfBodyPart = this.getColorOfJoint(bodyPart);
                        double fieldToShow = -1;
                        for(int k = 0; k < bodies.Length; k++)
                        {
                            if(currColorToView[k,0] == -1)
                            {
                                currColorToView[k, 0] = bodies[bodyIndex].TrackingId;
                                currColorToView[k, 1] = avgColorView;
                                fieldToShow = currColorToView[k, 1];
                                if(avgColorView == 5)
                                {
                                    avgColorView = 0;
                                }
                                else
                                {
                                   avgColorView++;
                                }
                                break;
                            }else if (currColorToView[k, 0] == bodies[bodyIndex].TrackingId)
                            {
                                fieldToShow = currColorToView[k, 1];
                                break;
                            }
                        }
                        // output to user
                        switch (fieldToShow)
                        {
                            case 0:
                                switch (j)
                                {
                                    case 0:
                                        mainWindow.ColorBoxFirstJoint1.Background = new SolidColorBrush(Color.FromRgb(colorOfBodyPart[0], colorOfBodyPart[1], colorOfBodyPart[2]));
                                        break;
                                    case 1:
                                        mainWindow.ColorBoxSecondJoint1.Background = new SolidColorBrush(Color.FromRgb(colorOfBodyPart[0], colorOfBodyPart[1], colorOfBodyPart[2]));
                                        break;
                                }
                                break;
                            case 1:
                                switch (j)
                                {
                                    case 0:
                                        mainWindow.ColorBoxFirstJoint2.Background = new SolidColorBrush(Color.FromRgb(colorOfBodyPart[0], colorOfBodyPart[1], colorOfBodyPart[2]));
                                        break;
                                    case 1:
                                        mainWindow.ColorBoxSecondJoint2.Background = new SolidColorBrush(Color.FromRgb(colorOfBodyPart[0], colorOfBodyPart[1], colorOfBodyPart[2]));
                                        break;
                                }
                                break;
                            case 2:
                                switch (j)
                                {
                                    case 0:
                                        mainWindow.ColorBoxFirstJoint3.Background = new SolidColorBrush(Color.FromRgb(colorOfBodyPart[0], colorOfBodyPart[1], colorOfBodyPart[2]));
                                        break;
                                    case 1:
                                        mainWindow.ColorBoxSecondJoint3.Background = new SolidColorBrush(Color.FromRgb(colorOfBodyPart[0], colorOfBodyPart[1], colorOfBodyPart[2]));
                                        break;
                                }
                                break;
                            case 3:
                                switch (j)
                                {
                                    case 0:
                                        mainWindow.ColorBoxFirstJoint4.Background = new SolidColorBrush(Color.FromRgb(colorOfBodyPart[0], colorOfBodyPart[1], colorOfBodyPart[2]));
                                        break;
                                    case 1:
                                        mainWindow.ColorBoxSecondJoint4.Background = new SolidColorBrush(Color.FromRgb(colorOfBodyPart[0], colorOfBodyPart[1], colorOfBodyPart[2]));
                                        break;
                                }
                                break;
                            case 4:
                                switch (j)
                                {
                                    case 0:
                                        mainWindow.ColorBoxFirstJoint5.Background = new SolidColorBrush(Color.FromRgb(colorOfBodyPart[0], colorOfBodyPart[1], colorOfBodyPart[2]));
                                        break;
                                    case 1:
                                        mainWindow.ColorBoxSecondJoint5.Background = new SolidColorBrush(Color.FromRgb(colorOfBodyPart[0], colorOfBodyPart[1], colorOfBodyPart[2]));
                                        break;
                                }
                                break;
                            case 5:
                                switch (j)
                                {
                                    case 0:
                                        mainWindow.ColorBoxFirstJoint6.Background = new SolidColorBrush(Color.FromRgb(colorOfBodyPart[0], colorOfBodyPart[1], colorOfBodyPart[2]));
                                        break;
                                    case 1:
                                        mainWindow.ColorBoxSecondJoint6.Background = new SolidColorBrush(Color.FromRgb(colorOfBodyPart[0], colorOfBodyPart[1], colorOfBodyPart[2]));
                                        break;
                                }
                                break;
                        }

                        // save it
                        this.colors[bodies[bodyIndex].TrackingId][j].Add(colorOfBodyPart);
               }
             }
                
           }
         }

         byte[] PostAvgColor(List<byte[]> colors, int[] preColors, double intError)
         {
           //195075 == (255^2)*3
           double error = intError;
           int overallError = (int) (error*195075);
           int eachError = (int)(error * 65025);
            
            int red = 0;
           int green = 0;
           int blue = 0;
           //int opacity = 0;
           int length = 0;
            int lengthForCheck = 0;
            
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
             if (comparison > preColors[3] - overallError && comparison < preColors[3] + overallError)
             {
               if (redTemp > preColors[0] - eachError * 1.5 && redTemp < preColors[0] + eachError * 1.5)
               {
                 if (greenTemp > preColors[1] - eachError * 1.5 && greenTemp < preColors[1] + eachError * 1.5)
                 {
                   if (blueTemp > preColors[2] - eachError * 1.5 && blueTemp < preColors[2] + eachError * 1.5)
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

            if (length != 0)
            {
                byte[] asarray = {
               (byte)Math.Sqrt(red / length),
               (byte)Math.Sqrt(green / length),
               (byte)Math.Sqrt(blue / length),
               //(byte)Math.Sqrt(opacity / length)
               255
             };
                //If less than three quarters of all Points are in the Span
                if (asarray.Length < lengthForCheck * 0.75)
                {
                    asarray = PostAvgColor(colors, preColors, (double)(intError * 5 / 4));
                }

                return asarray;
            }
            else
            {
                byte[] asarray = PostAvgColor(colors, preColors, (double)(intError * 5 / 4));
                return asarray;
            }

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
