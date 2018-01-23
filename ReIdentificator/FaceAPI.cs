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
using Microsoft.Kinect.Face;
using System.Windows.Controls;
using System.Drawing.Imaging;
using System.Drawing;



namespace ReIdentificator
{
    class FaceAPI
    {
        private static JointType[] jointTypesToTrack = { JointType.ShoulderLeft, JointType.ShoulderRight, JointType.ShoulderRight, JointType.KneeRight };
       //To be cleared? private int avgColorView = 0;
        private KinectSensor kinect;
        private Comparer comparer;
        private int counter = 0;
        private int counti = 0;
        private byte[] colorPixels;
        private Body[] bodies = null;
        private WriteableBitmap colorBitmap;
        private BitmapSource image;
        private MainWindow mainWindow;
        private Dictionary<ulong, List<byte[]>[]> colors = new Dictionary<ulong, List<byte[]>[]>();
        private Dictionary<ulong, Boolean> faceData = new Dictionary<ulong, Boolean>();
        //Dictionary that saves all tracked id's bool = false means the person has not been tracked yet
        private Dictionary<ulong, Boolean> trackedBodies = new Dictionary<ulong, bool>();
        private readonly IFaceServiceClient faceServiceClient =
        new FaceServiceClient("21f520d419c34834b5b955354b524026", "https://westcentralus.api.cognitive.microsoft.com/face/v1.0");
        private List<FaceProcessor_face> facesToProcess= new List<FaceProcessor_face>();

        private FaceFrameReader _faceReader = null;
        private BodyFrameReader _bodyReader = null;
        private IList<Body> _bodies = null;
        private FaceFrameSource _faceSource = null;
        private FaceFrameResult result;
        private ulong currentID;

        Face[] faces;                   // The list of detected faces.
        String[] faceDescriptions;      // The list of descriptions for the detected faces.
        bool[] alreadyPrinted;

        public FaceAPI(KinectSensor kinect, Comparer comparer, MainWindow mainWindow)
        {
            this.kinect = kinect;
            this.comparer = comparer;
            this.mainWindow = mainWindow;
            this.bodies = new Body[this.kinect.BodyFrameSource.BodyCount];
            _bodyReader = kinect.BodyFrameSource.OpenReader();
            _bodyReader.FrameArrived += BodyReader_FrameArrived;
            _faceSource = new FaceFrameSource(kinect, 0,
                                              FaceFrameFeatures.LeftEyeClosed |
                                              FaceFrameFeatures.MouthOpen |
                                              FaceFrameFeatures.RightEyeClosed);
            this._faceReader = _faceSource.OpenReader();
            _faceReader.FrameArrived += FaceReader_FrameArrived;
            _bodies = new Body[kinect.BodyFrameSource.BodyCount];
            mainWindow.BodyLeftView += HandleBodyLeftViewEvent;
        }
        /*Helper Functions that are needed to determine if there is a face in the frame
 */
        private void BodyReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    frame.GetAndRefreshBodyData(_bodies);

                    Body body = _bodies.Where(b => b.IsTracked).FirstOrDefault();

                    if (!_faceSource.IsTrackingIdValid)
                    {
                        if (body != null)
                        {
                            // Assign a tracking ID to the face source
                            _faceSource.TrackingId = body.TrackingId;
                            if (!trackedBodies.ContainsKey(body.TrackingId))
                            {
                                trackedBodies.Add(body.TrackingId, false);
                                currentID = body.TrackingId;
                            }
                        }
                    }
                }
            }
        }
        private void FaceReader_FrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {

                    // 4) Get the face frame result
                    result = frame.FaceFrameResult;
                    if (result != null)
                    {
                        // 5) Do magic!
                        // Get the face points, mapped in the color space.
                        var eyeLeft = result.FacePointsInColorSpace[FacePointType.EyeLeft];
                        var eyeRight = result.FacePointsInColorSpace[FacePointType.EyeRight];
                        var nose = result.FacePointsInColorSpace[FacePointType.Nose];
                        var mouthLeft = result.FacePointsInColorSpace[FacePointType.MouthCornerLeft];
                        var mouthRight = result.FacePointsInColorSpace[FacePointType.MouthCornerRight];

                        var eyeLeftClosed = result.FaceProperties[FaceProperty.LeftEyeClosed];
                        var eyeRightClosed = result.FaceProperties[FaceProperty.RightEyeClosed];
                        var mouthOpen = result.FaceProperties[FaceProperty.MouthOpen];
                    }

                }

            }
        }
        private bool IsFaceInFrame()
        {
            if (result != null) return true;
            else return false;

        }
        public bool ContainsKeyValue()
        {
            foreach (KeyValuePair<ulong,bool> item in trackedBodies)
            {
                if (item.Key == currentID && item.Value == false)
                {
                    trackedBodies[item.Key] = true;
                    return true;
                } 
            }
            return false;
        }

        private void SaveImage(BitmapSource image, int i)
        {

            string currentDir = Environment.CurrentDirectory;

            string subPath = currentDir + @"\Test"; // your code goes here

            bool exists = System.IO.Directory.Exists(subPath);

            if (!exists)
                System.IO.Directory.CreateDirectory(subPath);

            FileStream stream = new FileStream(String.Format(subPath+@"\Test{0}.jpg",counti), FileMode.Create);
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();

            encoder.FlipHorizontal = false;
            encoder.FlipVertical = false;
            encoder.QualityLevel = 30;
            encoder.Frames.Add(BitmapFrame.Create(image));
            encoder.Save(stream);

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
            if (IsFaceInFrame() == true && ContainsKeyValue()==true)
            {

                // 32-bit per pixel, RGBA image
                //PlanarImage Image = e.ImageFrame.Image;
                this.colorPixels = new byte[this.kinect.ColorFrameSource.FrameDescription.LengthInPixels * 4];
                colorFrame.CopyConvertedFrameDataToArray(this.colorPixels, Microsoft.Kinect.ColorImageFormat.Bgra);
                //int n = this.kinect.ColorFrameSource.FrameDescription.Width;
                //int b = System.Convert.ToInt32(this.kinect.ColorFrameSource.FrameDescription.BytesPerPixel);

                this.colorBitmap = new WriteableBitmap(this.kinect.ColorFrameSource.FrameDescription.Width, this.kinect.ColorFrameSource.FrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

                image = BitmapSource.Create(
                    this.kinect.ColorFrameSource.FrameDescription.Width, this.kinect.ColorFrameSource.FrameDescription.Height, 96, 96, PixelFormats.Bgr32, null, this.colorPixels, this.colorBitmap.PixelWidth * sizeof(int));



                mainWindow.FacePhoto2.Source = image;
                SaveImage(image, counti);
                counti = counti + 1;

                //SaveImage(image2,  counti);
                //  counti = counti + 1;
                //BrowseButton_Click2(counti);

                bodyFrame.GetAndRefreshBodyData(this.bodies);
                GetPositionOfHead(bodies);
            }

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

                /* !!!mainWindow.FacePhoto.Source = this.colorBitmap;*/


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


        private void processFace(FaceProcessor_face _face, Face face)
        {
            
            _face.face_gender = face.FaceAttributes.Gender;    
            _face.face_age = face.FaceAttributes.Age;
            _face.face_hair_bald = face.FaceAttributes.Hair.Bald * 100;
            HairColor[] hairColors = face.FaceAttributes.Hair.HairColor;
            foreach (HairColor hairColor in hairColors)
            {
                Debug.WriteLine(hairColor.Color.ToString());
                if (hairColor.Color.ToString() == "Blond")
                {
                    _face.face_hair_blonde = hairColor.Confidence * 100;
                }
                else if (hairColor.Color.ToString() == "Black")
                {
                    _face.face_hair_black = hairColor.Confidence * 100;
                }
                else if (hairColor.Color.ToString() == "Brown")
                {
                    _face.face_hair_brown = hairColor.Confidence * 100;
                }
                else if (hairColor.Color.ToString() == "Red")
                {
                    _face.face_hair_red = hairColor.Confidence * 100;
                }
                _face.face_glasses = face.FaceAttributes.Glasses.ToString();
            }
                mainWindow.printLog("body parameters: " + _face.face_gender + " - " + _face.face_age + " - " + _face.face_hair_bald + " - " + _face.face_hair_blonde + " - " + _face.face_hair_black + " - " + _face.face_hair_brown + " - " + _face.face_hair_red + " - " + _face.face_glasses);
                //mainWindow.startComparison(_body.TrackingId, _body);

            
        }


        public async void BrowseButton_Click2(int counti, FaceProcessor_face _face)
        {
            string currentDir = Environment.CurrentDirectory;

            string subPath = currentDir + @"\Test"; // your code goes here

            // Display the image file.
            string filePath = String.Format(subPath + @"\Test{0}.jpg", counti);

            Uri fileUri = new Uri(filePath);
            BitmapImage bitmapSource = new BitmapImage();

            bitmapSource.BeginInit();
            bitmapSource.CacheOption = BitmapCacheOption.None;
            bitmapSource.UriSource = fileUri;
            bitmapSource.EndInit();

            //mainWindow.FacePhoto.Source = bitmapSource;


            // Detect any faces in the image.
            mainWindow.Title = "Detecting...";
            faces = await UploadAndDetectFaces(filePath);
            mainWindow.Title = String.Format("Detection Finished. {0} face(s) detected", faces.Length);

            if (faces.Length > 0)
            {
                faceDescriptions = new String[faces.Length];
                alreadyPrinted = new bool[faces.Length];

                for (int i = 0; i < faces.Length; i++)
                {
                    Face face = faces[i];
                    
                    // Store the face description.
                    faceDescriptions[i] = FaceDescription(face);
                    alreadyPrinted[i] = false;

                    processFace(_face, face);
                }

                /*// Display the image with the rectangle around the face.
                RenderTargetBitmap faceWithRectBitmap = new RenderTargetBitmap(
                    (int)(bitmapSource.PixelWidth * resizeFactor),
                    (int)(bitmapSource.PixelHeight * resizeFactor),
                    96,
                    96,
                    PixelFormats.Pbgra32);*/

                
                //mainWindow.FacePhoto.Source = faceWithRectBitmap;
            }
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
                    
                    mainWindow.printLog(FaceDescription(faces[0]));

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

        private string FaceData(Face face)
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

        public float[] getPositionOfJoint(Joint joint)
        {
            ColorSpacePoint positionPoint = kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);
            if (between(positionPoint.X, 0, 1920) && between(positionPoint.Y, 0, 1080))
            {
           

                // extract color components
                float xval = positionPoint.X;
                float yval = positionPoint.Y;
            

            float[] asarray = { xval, yval };
            return asarray;
            }
            else
            {
                float[] asarray = { 0, 0};
                return asarray;
            }
        }

        void GetPositionOfHead(Body[] bodies)
        {
            for (int bodyIndex = 0; bodyIndex < bodies.Length; bodyIndex++)
            {
                Body body = bodies[bodyIndex];
                if (bodies[bodyIndex] != null && body.IsTracked)
                {
                    // save in this body's color timeseries
                    if (!this.faceData.ContainsKey(body.TrackingId))
                    {
                        this.faceData[body.TrackingId] = true;
                        facesToProcess.Add(new FaceProcessor_face(body.TrackingId));
                    }
                    FaceProcessor_face _face = facesToProcess.Find(element => element.TrackingId == body.TrackingId);

                    Joint head = bodies[bodyIndex].Joints[JointType.Head];
                    float[] positionOfHead = this.getPositionOfJoint(head);
                    mainWindow.printLog("positionOfHead[0]: " + positionOfHead[0] + " positionOfHead[1]: " + positionOfHead[1] + " minmax0" + (Math.Min(Math.Max((int)Math.Floor(positionOfHead[0]) - 100, 0), this.colorBitmap.PixelWidth)) + " minmax1: " + (Math.Min(Math.Max((int)Math.Floor(positionOfHead[1]) - 100, 0), this.colorBitmap.PixelHeight)));
                        Int32Rect faceRect = new Int32Rect((Math.Min(Math.Max((int)Math.Floor(positionOfHead[0]) - 100, 0), this.colorBitmap.PixelWidth)), (Math.Min(Math.Max((int)Math.Floor(positionOfHead[1]) - 100, 0), this.colorBitmap.PixelHeight)), 200, 200);
                        image.CopyPixels(faceRect, this.colorPixels, this.colorBitmap.PixelWidth * sizeof(int), 0);

                        BitmapSource image2 = BitmapSource.Create(200, 200, 96, 96, PixelFormats.Bgr32, null, this.colorPixels, this.colorBitmap.PixelWidth * sizeof(int));

                        mainWindow.FacePhoto.Source = image2;

                        SaveImage(image2, counti);
                        BrowseButton_Click2(counti, _face);

                        counti = counti + 1;
                    
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

        private void HandleBodyLeftViewEvent(object sender, LeftViewEventArgs e)
        {

            FaceProcessor_face face = facesToProcess.Find(element => element.TrackingId == e.TrackingId);
            if (face != null)
            {
                mainWindow.startComparison(face.TrackingId, face);
            }
            facesToProcess.Remove(face);
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
    }

    class FaceProcessor_face
    {
        public ulong TrackingId { get; set; }
        public string face_gender { get; set; } // -1 if not detected properly
        public double face_age { get; set; }
        public double face_hair_bald { get; set; }
        public double face_hair_blonde { get; set; }
        public double face_hair_black { get; set; }
        public double face_hair_brown { get; set; }
        public double face_hair_red { get; set; }
        public string face_glasses { get; set; }

        public FaceProcessor_face(ulong trackingId)
        {
            this.TrackingId = trackingId;
        }
    }
}
