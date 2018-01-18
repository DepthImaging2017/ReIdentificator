using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
//using Windows.Graphic.Imaging;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System.Diagnostics;
using System.Linq;
using Microsoft.Kinect;
using System.Windows.Controls;
using System.Drawing.Imaging;
using System.Drawing;



namespace ReIdentificator
{
    class FaceAPI
    {
        private double deleteTheTopAndBottom = 0.05;
        private static JointType[] jointTypesToTrack = { JointType.ShoulderLeft, JointType.ShoulderRight, JointType.ShoulderRight, JointType.KneeRight };
        private int avgColorView = 0;
        private ulong[,] currColorToView = new ulong[6, 2] { {  0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 } };
        private KinectSensor kinect;
        private Comparer comparer;
        private int counter = 0;
        private byte[] colorPixels;
        private Body[] bodies = null;
        private WriteableBitmap colorBitmap;
        private Bitmap secBitmap;
        private MainWindow mainWindow;
        private Dictionary<ulong, List<byte[]>[]> colors = new Dictionary<ulong, List<byte[]>[]>();
        private readonly IFaceServiceClient faceServiceClient =
            new FaceServiceClient("21f520d419c34834b5b955354b524026", "https://westcentralus.api.cognitive.microsoft.com/face/v1.0");

        Face[] faces;                   // The list of detected faces.
        String[] faceDescriptions;      // The list of descriptions for the detected faces.
        bool[] alreadyPrinted;
        double resizeFactor;            // The resize factor for the displayed image.

        public FaceAPI(KinectSensor kinect, Comparer comparer, MainWindow mainWindow)
        {
            this.kinect = kinect;
            this.comparer = comparer;
            this.mainWindow = mainWindow;
            this.bodies = new Body[this.kinect.BodyFrameSource.BodyCount];
        }



        private void SaveImage(BitmapSource image)
        {
            FileStream stream = new FileStream(@"C:\Users\Benjamin Karic\Desktop\TEST\TEST.jpg", FileMode.Create);
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();

            encoder.FlipHorizontal = true;
            encoder.FlipVertical = false;
            encoder.QualityLevel = 30;
            encoder.Frames.Add(BitmapFrame.Create(image));
            encoder.Save(stream);
            mainWindow.printLog("Saved Stream.");

            stream.Close();
        }




        //Call this method after you created your image in the eventhandlers (e.g.)
        
        /// <summary>
        /// Eventhandler, triggered when new color frame is available
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void nui_ColorFrameReady(ColorFrame colorFrame, BodyFrame bodyFrame)
        {
                
                // 32-bit per pixel, RGBA image
                //PlanarImage Image = e.ImageFrame.Image;
                this.colorPixels = new byte[this.kinect.ColorFrameSource.FrameDescription.LengthInPixels * 4];
                colorFrame.CopyConvertedFrameDataToArray(this.colorPixels, Microsoft.Kinect.ColorImageFormat.Bgra);
                int n = this.kinect.ColorFrameSource.FrameDescription.Width;
                int b = System.Convert.ToInt32(this.kinect.ColorFrameSource.FrameDescription.BytesPerPixel);
                this.colorBitmap = new WriteableBitmap(this.kinect.ColorFrameSource.FrameDescription.Width, this.kinect.ColorFrameSource.FrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
                BitmapSource image = BitmapSource.Create(
                this.kinect.ColorFrameSource.FrameDescription.Width, this.kinect.ColorFrameSource.FrameDescription.Height, 96, 96, PixelFormats.Bgr32, null, this.colorPixels, this.colorBitmap.PixelWidth * sizeof(int));

                mainWindow.FacePhoto2.Source = image;

                SaveImage(image);
            BrowseButton_Click2();
        }



        public void processFaceFrame(ColorFrame colorFrame, BodyFrame bodyFrame)
        {
            if (counter % 100 == 50)
            {
                this.colorPixels = new byte[this.kinect.ColorFrameSource.FrameDescription.LengthInPixels * 4];
                colorFrame.CopyConvertedFrameDataToArray(this.colorPixels, Microsoft.Kinect.ColorImageFormat.Bgra);
                this.colorBitmap = new WriteableBitmap(this.kinect.ColorFrameSource.FrameDescription.Width, this.kinect.ColorFrameSource.FrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
                //this.secBitmap = new Bitmap(this.kinect.ColorFrameSource.FrameDescription.Width, this.kinect.ColorFrameSource.FrameDescription.Height, colorBitmap.PixelWidth * sizeof(int), System.Drawing.Imaging.PixelFormat.Format32bppRgb, colorPixels);
                // Write the pixel data into our bitmap
                this.colorBitmap.WritePixels(
                    new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                    this.colorPixels,
                    this.colorBitmap.PixelWidth * sizeof(int),
                    0);

                mainWindow.FacePhoto.Source = this.colorBitmap;


                /*SoftwareBitmap outputBitmap = SoftwareBitmap.CreateCopyFromBuffer(
                writeableBitmap.PixelBuffer,
                BitmapPixelFormat.Bgra8,
                writeableBitmap.PixelWidth,
                writeableBitmap.PixelHeight
                );*/



                Bitmap myBitmap;
                ImageCodecInfo myImageCodecInfo;
                System.Drawing.Imaging.Encoder myEncoder;
                EncoderParameter myEncoderParameter;
                EncoderParameters myEncoderParameters;

                // Create a Bitmap object based on a BMP file.
                //myBitmap = /*new Bitmap("C:/Users/Benjamin Karic/Downloads/Soviet_BMP-1_IFV.bmp");*/ BitmapFromWriteableBitmap(this.colorBitmap);
                

                // Get an ImageCodecInfo object that represents the JPEG codec.
                myImageCodecInfo = GetEncoderInfo("image/jpeg");

                // Create an Encoder object based on the GUID

                // for the Quality parameter category.
                myEncoder = System.Drawing.Imaging.Encoder.Quality;

                // Create an EncoderParameters object.

                // An EncoderParameters object has an array of EncoderParameter

                // objects. In this case, there is only one

                // EncoderParameter object in the array.
                myEncoderParameters = new EncoderParameters(1);

                // Save the bitmap as a JPEG file with quality level 25.
                myEncoderParameter = new EncoderParameter(myEncoder, 25L);
                myEncoderParameters.Param[0] = myEncoderParameter;
                //cloneBitmap.Save("C:/Users/Benjamin Karic/Codepicture"+ counter +".jpg", myImageCodecInfo, myEncoderParameters);
                

            }
            counter = counter + 1;
            //bodyFrame.GetAndRefreshBodyData(this.bodies);
            //GetColorOfBodyParts(jointTypesToTrack, bodies);
        }


        private Bitmap BitmapFromWriteableBitmap(WriteableBitmap writeBmp)
        {
            Bitmap bmp;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create((BitmapSource)writeBmp));
                enc.Save(outStream);
                bmp = new Bitmap(outStream);
            }
            return bmp;
        }

        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }


        public async void BrowseButton_Click1(object sender, RoutedEventArgs e)
        {
            // Get the image file to scan from the user.
           // var openDlg = new Microsoft.Win32.OpenFileDialog();

            //openDlg.Filter = "JPEG Image(*.jpg)|*.jpg";
            //bool? result = openDlg.ShowDialog(this.mainWindow);

            // Return if canceled.
           // if (!(bool)result)
            //{
             //   return;
            //}

            // Display the image file.
            string filePath = @"C:\Users\Benjamin Karic\Desktop\TEST\TEST.jpg";

            Uri fileUri = new Uri(filePath);
            BitmapImage bitmapSource = new BitmapImage();

            bitmapSource.BeginInit();
            bitmapSource.CacheOption = BitmapCacheOption.None;
            bitmapSource.UriSource = fileUri;
            bitmapSource.EndInit();

            mainWindow.FacePhoto.Source = bitmapSource;
            

            // Detect any faces in the image.
            mainWindow.Title = "Detecting...";
            faces = await UploadAndDetectFaces(filePath);
            mainWindow.Title = String.Format("Detection Finished. {0} face(s) detected", faces.Length);

            if (faces.Length > 0)
            {
                // Prepare to draw rectangles around the faces.
                DrawingVisual visual = new DrawingVisual();
                DrawingContext drawingContext = visual.RenderOpen();
                drawingContext.DrawImage(bitmapSource,
                    new Rect(0, 0, bitmapSource.Width, bitmapSource.Height));
                double dpi = bitmapSource.DpiX;
                resizeFactor = 96 / dpi;
                faceDescriptions = new String[faces.Length];
                alreadyPrinted = new bool[faces.Length];

                for (int i = 0; i < faces.Length; ++i)
                {
                    Face face = faces[i];

                    // Draw a rectangle on the face.
                    drawingContext.DrawRectangle(
                        System.Windows.Media.Brushes.Transparent,
                        new System.Windows.Media.Pen(System.Windows.Media.Brushes.Red, 2),
                        new Rect(
                            face.FaceRectangle.Left * resizeFactor,
                            face.FaceRectangle.Top * resizeFactor,
                            face.FaceRectangle.Width * resizeFactor,
                            face.FaceRectangle.Height * resizeFactor
                            )
                    );



                    // Store the face description.
                    faceDescriptions[i] = FaceDescription(face);
                    alreadyPrinted[i] = false;
                }

                drawingContext.Close();

                // Display the image with the rectangle around the face.
                RenderTargetBitmap faceWithRectBitmap = new RenderTargetBitmap(
                    (int)(bitmapSource.PixelWidth * resizeFactor),
                    (int)(bitmapSource.PixelHeight * resizeFactor),
                    96,
                    96,
                    PixelFormats.Pbgra32);

                faceWithRectBitmap.Render(visual);
               mainWindow.FacePhoto.Source = faceWithRectBitmap;

                // Set the status bar text.
                mainWindow.faceDescriptionStatusBar.Text = "Place the mouse pointer over a face to see the face description.";
            }
        }



        public async void BrowseButton_Click2()
        {
            // Get the image file to scan from the user.
            // var openDlg = new Microsoft.Win32.OpenFileDialog();

            //openDlg.Filter = "JPEG Image(*.jpg)|*.jpg";
            //bool? result = openDlg.ShowDialog(this.mainWindow);

            // Return if canceled.
            // if (!(bool)result)
            //{
            //   return;
            //}

            // Display the image file.
            string filePath = @"C:\Users\Benjamin Karic\Desktop\TEST\TEST.jpg";

            Uri fileUri = new Uri(filePath);
            BitmapImage bitmapSource = new BitmapImage();

            bitmapSource.BeginInit();
            bitmapSource.CacheOption = BitmapCacheOption.None;
            bitmapSource.UriSource = fileUri;
            bitmapSource.EndInit();

            mainWindow.FacePhoto.Source = bitmapSource;


            // Detect any faces in the image.
            mainWindow.Title = "Detecting...";
            faces = await UploadAndDetectFaces(filePath);
            mainWindow.Title = String.Format("Detection Finished. {0} face(s) detected", faces.Length);

            if (faces.Length > 0)
            {
                // Prepare to draw rectangles around the faces.
                DrawingVisual visual = new DrawingVisual();
                DrawingContext drawingContext = visual.RenderOpen();
                drawingContext.DrawImage(bitmapSource,
                    new Rect(0, 0, bitmapSource.Width, bitmapSource.Height));
                double dpi = bitmapSource.DpiX;
                resizeFactor = 96 / dpi;
                faceDescriptions = new String[faces.Length];
                alreadyPrinted = new bool[faces.Length];

                for (int i = 0; i < faces.Length; ++i)
                {
                    Face face = faces[i];

                    // Draw a rectangle on the face.
                    drawingContext.DrawRectangle(
                        System.Windows.Media.Brushes.Transparent,
                        new System.Windows.Media.Pen(System.Windows.Media.Brushes.Red, 2),
                        new Rect(
                            face.FaceRectangle.Left * resizeFactor,
                            face.FaceRectangle.Top * resizeFactor,
                            face.FaceRectangle.Width * resizeFactor,
                            face.FaceRectangle.Height * resizeFactor
                            )
                    );



                    // Store the face description.
                    faceDescriptions[i] = FaceDescription(face);
                    alreadyPrinted[i] = false;
                }

                drawingContext.Close();

                // Display the image with the rectangle around the face.
                RenderTargetBitmap faceWithRectBitmap = new RenderTargetBitmap(
                    (int)(bitmapSource.PixelWidth * resizeFactor),
                    (int)(bitmapSource.PixelHeight * resizeFactor),
                    96,
                    96,
                    PixelFormats.Pbgra32);

                faceWithRectBitmap.Render(visual);
                mainWindow.FacePhoto.Source = faceWithRectBitmap;

                // Set the status bar text.
                mainWindow.faceDescriptionStatusBar.Text = "Place the mouse pointer over a face to see the face description.";
            }
        }

        public void FacePhoto_MouseMove1(object sender, MouseEventArgs e)
        {
            // If the REST call has not completed, return from this method.
            if (faces == null)
                return;

            // Find the mouse position relative to the image.
            System.Windows.Point mouseXY = e.GetPosition(mainWindow.FacePhoto);

            ImageSource imageSource = mainWindow.FacePhoto.Source;
            BitmapSource bitmapSource = (BitmapSource)imageSource;

            // Scale adjustment between the actual size and displayed size.
            var scale = mainWindow.FacePhoto.ActualWidth / (bitmapSource.PixelWidth / resizeFactor);

            // Check if this mouse position is over a face rectangle.
            bool mouseOverFace = false;

            for (int i = 0; i < faces.Length; ++i)
            {
                FaceRectangle fr = faces[i].FaceRectangle;
                double left = fr.Left * scale;
                double top = fr.Top * scale;
                double width = fr.Width * scale;
                double height = fr.Height * scale;

                // Display the face description for this face if the mouse is over this face rectangle.
                if (mouseXY.X >= left && mouseXY.X <= left + width && mouseXY.Y >= top && mouseXY.Y <= top + height)
                {   
                    if (alreadyPrinted[i]) {
                        return;
                    }
                    mainWindow.printLog(faceDescriptions[i]);
                    mouseOverFace = true;
                    alreadyPrinted[i] = true;
                    break;
                }
                else
                {
                    alreadyPrinted[i] = false;
                }
            }

            // If the mouse is not over a face rectangle.
            if (!mouseOverFace)
                mainWindow.faceDescriptionStatusBar.Text = "Place the mouse pointer over a face to see the face description.";
                
        }

        private async Task<Face[]> UploadAndDetectFaces(string imageFilePath)
        {
            // The list of Face attributes to return.
            IEnumerable<FaceAttributeType> faceAttributes =
                new FaceAttributeType[] { FaceAttributeType.Gender, FaceAttributeType.Age, FaceAttributeType.Smile, FaceAttributeType.Emotion, FaceAttributeType.Glasses, FaceAttributeType.Hair };

            // Call the Face API.
            try
            {
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    Face[] faces = await faceServiceClient.DetectAsync(imageFileStream, returnFaceId: true, returnFaceLandmarks: false, returnFaceAttributes: faceAttributes);
                    return faces;
                }
            }
            // Catch and display Face API errors.
            catch (FaceAPIException f)
            {
                MessageBox.Show(f.ErrorMessage, f.ErrorCode);
                return new Face[0];
            }
            // Catch and display all other errors.
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error");
                return new Face[0];
            }
        }

        private string FaceDescription(Face face)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Face: ");

            // Add the gender, age, and smile.
            sb.Append(face.FaceAttributes.Gender);
            sb.Append(", ");
            sb.Append(face.FaceAttributes.Age);
            sb.Append(", ");
            sb.Append(String.Format("smile {0:F1}%, ", face.FaceAttributes.Smile * 100));

            // Add the emotions. Display all emotions over 10%.
            sb.Append("Emotion: ");
            EmotionScores emotionScores = face.FaceAttributes.Emotion;
            if (emotionScores.Anger >= 0.1f) sb.Append(String.Format("anger {0:F1}%, ", emotionScores.Anger * 100));
            if (emotionScores.Contempt >= 0.1f) sb.Append(String.Format("contempt {0:F1}%, ", emotionScores.Contempt * 100));
            if (emotionScores.Disgust >= 0.1f) sb.Append(String.Format("disgust {0:F1}%, ", emotionScores.Disgust * 100));
            if (emotionScores.Fear >= 0.1f) sb.Append(String.Format("fear {0:F1}%, ", emotionScores.Fear * 100));
            if (emotionScores.Happiness >= 0.1f) sb.Append(String.Format("happiness {0:F1}%, ", emotionScores.Happiness * 100));
            if (emotionScores.Neutral >= 0.1f) sb.Append(String.Format("neutral {0:F1}%, ", emotionScores.Neutral * 100));
            if (emotionScores.Sadness >= 0.1f) sb.Append(String.Format("sadness {0:F1}%, ", emotionScores.Sadness * 100));
            if (emotionScores.Surprise >= 0.1f) sb.Append(String.Format("surprise {0:F1}%, ", emotionScores.Surprise * 100));

            // Add glasses.
            sb.Append(face.FaceAttributes.Glasses);
            sb.Append(", ");

            // Add hair.
            sb.Append("Hair: ");

            // Display baldness confidence if over 1%.
            if (face.FaceAttributes.Hair.Bald >= 0.01f)
                sb.Append(String.Format("bald {0:F1}% ", face.FaceAttributes.Hair.Bald * 100));

            // Display all hair color attributes over 10%.
            HairColor[] hairColors = face.FaceAttributes.Hair.HairColor;
            foreach (HairColor hairColor in hairColors)
            {
                if (hairColor.Confidence >= 0.1f)
                {
                    sb.Append(hairColor.Color.ToString());
                    sb.Append(String.Format(" {0:F1}% ", hairColor.Confidence * 100));
                }
            }

            // Return the built string.
            return sb.ToString();
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
