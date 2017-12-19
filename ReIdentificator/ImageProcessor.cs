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
using System.Windows.Controls;

namespace ReIdentificator
{
    class ImageProcessor
    {
        private double deleteTheTopAndBottom = 0.05;
        private static JointType[] jointTypesToTrack = { JointType.ShoulderLeft, JointType.ShoulderRight, JointType.ShoulderRight, JointType.KneeRight };
        private int avgColorView = 0;
        private ulong[,] currColorToView = new ulong[6, 2] { {  0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 } };
        private KinectSensor kinect;
        private Comparer comparer;
        private byte[] colorPixels;
        private Body[] bodies = null;
        private WriteableBitmap colorBitmap;
        private MainWindow mainWindow;
        private Dictionary<ulong, List<byte[]>[]> colors = new Dictionary<ulong, List<byte[]>[]>();

        public ImageProcessor(KinectSensor kinect, Comparer comparer, MainWindow mainWindow)
        {
            this.kinect = kinect;
            this.comparer = comparer;
            this.mainWindow = mainWindow;
            this.bodies = new Body[this.kinect.BodyFrameSource.BodyCount];
            mainWindow.StackPanelInit(jointTypesToTrack.Length);

            mainWindow.BodyLeftView += HandleBodyLeftViewEvent;
        }

        public byte[] getColorOfJoint(Joint joint)
        {
            ColorSpacePoint colorPoint = kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);
            if (between(colorPoint.X, 0, 1920) && between(colorPoint.Y, 0, 1080))
            {
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

            byte[] asarray = { red, green, blue, opacity };
            return asarray;
            }
            else
            {
                byte[] asarray = { 0, 0, 0, 0};
                return asarray;
            }
        }

        public void processColorFrame(ColorFrame colorFrame, BodyFrame bodyFrame)
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
            bodyFrame.GetAndRefreshBodyData(this.bodies);
            GetColorOfBodyParts(jointTypesToTrack, bodies);
        }

        byte[] averageColor(List<byte[]> colors)
        {
            int red = 0;
            int green = 0;
            int blue = 0;
            int opacity = 0;
            int length = 0;

            colors.ForEach(color => {
                if(color[3] != 0) {
                    red += (int)Math.Pow(color[0], 2);
                    green += (int)Math.Pow(color[1], 2);
                    blue += (int)Math.Pow(color[2], 2);
                    opacity += (int)Math.Pow(color[3], 2);
                    length++;
                }
            });

            byte[] asarray = {
                (byte)Math.Sqrt(red / length),
                (byte)Math.Sqrt(green / length),
                (byte)Math.Sqrt(blue / length),
                (byte)Math.Sqrt(opacity / length)
            };

            int[] indicator = IndicatorColor(asarray);
            asarray = PostAvgColor(colors, indicator, deleteTheTopAndBottom);

            return asarray;
        }

        void HandleBodyLeftViewEvent(object sender, LeftViewEventArgs e)
        {
            double fieldToShow = -1;
            Console.WriteLine("body with id " + e.TrackingId + " has left frame");
            for (int i = 0; i < currColorToView.GetLength(0); i++)
            {
                if (currColorToView[i, 0] == e.TrackingId)
                {
                    fieldToShow = currColorToView[i, 1];
                    for (int j = currColorToView.GetLength(0) - 1; j > i-1; j--)
                    {
                        if (currColorToView[j, 0] != 0)
                        {
                            currColorToView[i, 1] = currColorToView[j, 1];
                            currColorToView[i, 0] = currColorToView[j, 0];
                            currColorToView[j, 0] = 0;
                            currColorToView[j, 1] = 0;
                            break;
                        }
                    }
                    break;
                }
            }
            userOutput(e, fieldToShow);
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
             byte[,] outputColors = new byte[jointTypesToTrack.Length, 4];
             double fieldToShow = -1;

             if (bodies[bodyIndex] != null && bodies[bodyIndex].IsTracked)
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
                 for(int k = 0; k < colorOfBodyPart.Length; k++)
                 {
                   outputColors[j, k] = colorOfBodyPart[k];
                 }
                 if(colorOfBodyPart[3] != 0) {
                   for(int k = 0; k < bodies.Length; k++)
                   {
                     if (currColorToView[k,0] == 0)
                     {
                       currColorToView[k, 0] = bodies[bodyIndex].TrackingId;
                       currColorToView[k, 1] = (ulong) avgColorView;
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
                     // save it
                     this.colors[bodies[bodyIndex].TrackingId][j].Add(colorOfBodyPart);
                   }
                 }
                 if(fieldToShow != -1)
                    {
                        mainWindow.updatePanel(outputColors, fieldToShow);
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
                     if(color[3] != 0)
                     {
                        red += redTemp;
                        green += greenTemp;
                        blue += blueTemp;
                        length++;
                     }

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
         }

        public bool between(float x, int low, int high)
        {
            if (x <= high && x >= low)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void userOutput(LeftViewEventArgs e, double fieldToShow)
        {
            byte[,] outputColors = new byte[this.colors[e.TrackingId].Length, 4];
            for(int i = 0; i < this.colors[e.TrackingId].Length; i++)
            {
                byte[] avgColor = averageColor(this.colors[e.TrackingId][i]);
                for(int j = 0; j < outputColors.GetLength(1)-1; j++)
                {
                    outputColors[i,j] = avgColor[j];
                }
                mainWindow.printLog("average color of joint #"+(i+1)+" of person with id " + e.TrackingId + ": " + avgColor[0] + ", " + avgColor[1] + ", " + avgColor[2] + ", " + avgColor[3]);
            }
            mainWindow.updatePanel(outputColors, fieldToShow);
        }
    }
}
