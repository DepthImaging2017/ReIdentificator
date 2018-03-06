using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
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
        /*This is the key you will need in order for the API to work 
          we used trial keys which lasted 30 days and have a limit on how many 
          calls to the API you can make

            visit : https://azure.microsoft.com/de-de/services/cognitive-services/face/ for a new key 
        */  
        private static string API_KEY = "deae3f3a4846477cb7c31086a18fbe3a";
        private readonly IFaceServiceClient faceServiceClient =
        new FaceServiceClient(API_KEY, "https://westcentralus.api.cognitive.microsoft.com/face/v1.0");
        private static JointType[] jointTypesToTrack = { JointType.ShoulderLeft, JointType.ShoulderRight, JointType.ShoulderRight, JointType.KneeRight };
        private KinectSensor kinect;
        private Comparer comparer;
        // Counter that counts after each face uploading process, also used for name of temporary stored face images 
        private int counti = 0;
        private byte[] colorPixels;
        private Body[] bodies = null;
        private WriteableBitmap colorBitmap;
        private BitmapSource image;
        private ReIdent reident;
        private Dictionary<ulong, Boolean> faceData = new Dictionary<ulong, Boolean>();
        //Dictionary that saves all tracked id's bool = false means the person has not been tracked yet
        private Dictionary<ulong, Boolean> trackedBodies = new Dictionary<ulong, bool>();
        private List<FaceProcessor_face> facesToProcess= new List<FaceProcessor_face>();
        private FaceFrameReader _faceReader = null;
        private BodyFrameReader _bodyReader = null;
        private IList<Body> _bodies = null;
        private FaceFrameSource _faceSource = null;
        private ulong currentID ;
        private bool faceTracked = false; //Global variable that is used in the function IsFaceInFrame(result)
        Face[] faces;                   // The list of detected faces.

        public FaceAPI(KinectSensor kinect, Comparer comparer, ReIdent reident)
        {
            this.kinect = kinect;
            this.comparer = comparer;
            this.reident = reident;
            this.bodies = new Body[this.kinect.BodyFrameSource.BodyCount];
            _bodyReader = kinect.BodyFrameSource.OpenReader();
            _bodyReader.FrameArrived += BodyReader_FrameArrived;
            _faceSource = new FaceFrameSource(kinect, 0,  FaceFrameFeatures.BoundingBoxInColorSpace |
                                                          FaceFrameFeatures.FaceEngagement |
                                                          FaceFrameFeatures.Glasses |
                                                          FaceFrameFeatures.Happy |
                                                          FaceFrameFeatures.LeftEyeClosed |
                                                          FaceFrameFeatures.MouthOpen |
                                                          FaceFrameFeatures.PointsInColorSpace |
                                                          FaceFrameFeatures.RightEyeClosed);
            this._faceReader = _faceSource.OpenReader();
            _faceReader.FrameArrived += FaceReader_FrameArrived;
            _bodies = new Body[kinect.BodyFrameSource.BodyCount];
            reident.BodyLeftView += HandleBodyLeftViewEvent;
        }
        /*
         * Helper Functions that are needed to determine if there is a face in the frame
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
        public void FaceReader_FrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    // 4) Get the face frame result
                    FaceFrameResult result = frame.FaceFrameResult;

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
                        IsFaceInFrame(result);

                    }

                }

            }
        }

        /* 
         * Simple function that detects if the given face parameters are set or not 
         * more parameters can be added via '&&'
         */
        private void IsFaceInFrame(FaceFrameResult result)
        {
            if (result != null && result.FacePointsInColorSpace[FacePointType.EyeLeft].X > 0 &&
                result.FacePointsInColorSpace[FacePointType.EyeLeft].Y > 0 &&
                result.FacePointsInColorSpace[FacePointType.EyeRight].X > 0 &&
                result.FacePointsInColorSpace[FacePointType.EyeRight].Y > 0)
                faceTracked = true;
            else
                faceTracked = false;
               
        }

        /* 
         * Function that calculates distance between center of camera and a specific CameraSpacePoint
         * @param point is a specific CameraSpacePoint
         * returns distance of point to camera center in meters as double 
         */
        public double Length(CameraSpacePoint point)
        {
            return Math.Sqrt(
                (point.X * point.X) +
                (point.Y * point.Y) +
                (point.Z * point.Z)
            );
        }

        /*Function that calculates distance between center of camera and head joint of a given body
         * @param body a specific Kinect body object
         * returns distance between head joint of body and camera in meters as double
         */
        private double BodyDistanceToCameron (Body body)
        {
            /*set the initial distance between camera and body(headjoint) to 100 meters, as we know Kinect 
            can not track bodies of 100m -> this means if returned distance is still 100 the body was not tracked*/
            double distance = 100;
            //if a body is tracked...
            if (body != null)
            {
                //set point to the position of head joint 
                var point = body.Joints[JointType.Head].Position;
                //calculate distance between camera and head joint
                distance = Length(point);
                
            }
           
                return distance;
        } 

        public bool ContainsKeyValue()
        {
            foreach (KeyValuePair<ulong, bool> item in trackedBodies)
            {
                if (item.Key == currentID && item.Value == false)
                {
                    trackedBodies[item.Key] = true;
                    return true;
                }
            }
            return false;
        }

        /*
         * Function that saves a BitmapSource to a .jpeg file with name "Test" + a given integer
         * @param image is the BitmapSource you want to save to jpeg
         */
        private void SaveImage(BitmapSource image)
        {
            //check currrent Direction path
            string currentDir = Environment.CurrentDirectory;

            //sets the Path your .jpeg will be saved to
            string subPath = currentDir + @"\Test"; 

            //checks whether the needed directory was already created , if not it will be created here 
            bool exists = System.IO.Directory.Exists(subPath);
            if (!exists)
                System.IO.Directory.CreateDirectory(subPath);

            // create .jpeg from BitmapSource and save it to subpath
            FileStream stream = new FileStream(String.Format(subPath+@"\Test{0}.jpg",counti), FileMode.Create);
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.FlipHorizontal = false;
            encoder.FlipVertical = false;
            encoder.QualityLevel = 30;
            encoder.Frames.Add(BitmapFrame.Create(image));
            encoder.Save(stream);

            //close stream
            stream.Close();
        }

        
        //Call this method after you created your image in the eventhandlers (e.g.)
        public void processFaceAnalysis(ColorFrame colorFrame, BodyFrame bodyFrame)
        {
            // if there is a face in current frame
            if (faceTracked == true) { 
                Debug.WriteLine("Alle Parameter erfüllt");

                //create empty byte array with size of colorFrame (RGBA image, 32 bit per pixel -> byte array with size=(Length in Pixels * 4))
                this.colorPixels = new byte[this.kinect.ColorFrameSource.FrameDescription.LengthInPixels * 4];
                
                //copy data from color frame to colorPixels byte array
                colorFrame.CopyConvertedFrameDataToArray(this.colorPixels, Microsoft.Kinect.ColorImageFormat.Bgra);

                //create WriteAbleBitmap with size of color frame source
                this.colorBitmap = new WriteableBitmap(this.kinect.ColorFrameSource.FrameDescription.Width, this.kinect.ColorFrameSource.FrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

                //set BitmapSource image to ColorFrameSource
                image = BitmapSource.Create(
                    this.kinect.ColorFrameSource.FrameDescription.Width, this.kinect.ColorFrameSource.FrameDescription.Height, 96, 96, PixelFormats.Bgr32, null, this.colorPixels, this.colorBitmap.PixelWidth * sizeof(int));


                //shows our "image" in the reident window
                reident.FacePhoto2.Source = image;

                bodyFrame.GetAndRefreshBodyData(this.bodies);
                GetPositionOfHead(bodies);
            

            }
        }

        /** 
         * Helper function that deletes every picture in the given directory
         * gets called after an API call is returned 
         */
        public void DeletePictures()
        {
            string currentDir = Environment.CurrentDirectory;

            System.IO.DirectoryInfo di = new DirectoryInfo(currentDir + @"\Test");

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        /*
         * Function that sets attributes of a FaceProcessor_face object to values from a face analyzed by Microsoft Azure FaceAPI
         * @param _face is a specific Object that will be filled with data
         * @param face is an object given by Microsoft Azure FaceAPI that includes calculated facedata 
         */
        private void processFace(FaceProcessor_face _face, Face face)
        {
            
            //set all attributes from _face to given values of face 
            _face.face_gender = face.FaceAttributes.Gender;    
            _face.face_age = face.FaceAttributes.Age;
            _face.face_hair_bald = face.FaceAttributes.Hair.Bald * 100;
            
            //set haircolor attributes
            HairColor[] hairColors = face.FaceAttributes.Hair.HairColor;
            foreach (HairColor hairColor in hairColors)
            {
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
        }

        /*
         * Helper Function that that detects path of image and calls functions "UploadAndDetectFaces" to send it to Microsoft Azure FaceAPI and function "processFace" to saves result of face analysis to own face objects
         * @param _face is a specific Object that will be filled with face data
         */
        public async void SendPictureToAPI(FaceProcessor_face _face)
        {
            //get current working direction
            string currentDir = Environment.CurrentDirectory;
            string subPath = currentDir + @"\Test"; // your code goes here

            // Display the image file.
            string filePath = String.Format(subPath + @"\Test{0}.jpg", counti);

            //send image to Microsoft Azure FaceAPI & detect any faces in the image.
            faces = await UploadAndDetectFaces(filePath);

            //if there are some faces.. (there should only be one face as we only send a rectangle with size of one face)
            if (faces.Length > 0)
            {
                for (int i = 0; i < faces.Length; i++)
                {   
                    //set attributes of our _face object to those returned by Microsoft Azure FaceAPI               
                    processFace(_face, faces[i]);
                }
            }
        }


        /*
         * Function that sends image and request to Microsoft Azure FaceAPI 
         * @param imageFilePath is the Path of the image to be uploaded to FaceAPI
         * returns array of faces that were found in the uploaded image 
         */
        private async Task<Face[]> UploadAndDetectFaces(string imageFilePath)
        {

            // The list of Face attributes to return.
            IEnumerable<FaceAttributeType> faceAttributes =
                new FaceAttributeType[] { FaceAttributeType.Gender, FaceAttributeType.Age, FaceAttributeType.Glasses, FaceAttributeType.Hair };

            // Call the Face API.
            try
            {
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                { 
                    Face[] faces = await faceServiceClient.DetectAsync(imageFileStream, returnFaceId: true, returnFaceLandmarks: false, returnFaceAttributes: faceAttributes);
                    
                    trackedBodies[currentID] = true;
                    //deletes temporary stored face image
                    DeletePictures();
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


        /*
         * Function that gets the 2D-Color-Image position of a body joint
         * @param joint is the joint to calculate the 2D Color coordinates from
         * returns 2D-Color-Image coordinates of the joint as an float array
         */
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


        /*
         * Function that determines each body in camera view, calculates the position of its head and takes a snapshot
         * only of the head section to send it to FaceAPI and process it 
         * @param bodies is an array of all bodies that are tracked by Kinect at the moment
         */
        void GetPositionOfHead(Body[] bodies)
        {
            for (int bodyIndex = 0; bodyIndex < bodies.Length; bodyIndex++)
            {

                Body body = bodies[bodyIndex];
                //Debug.WriteLine("Distanz zur Kamera: " + BodyDistanceToCameron(body));
                if (bodies[bodyIndex] != null && body.IsTracked && BodyDistanceToCameron(body) < 2.5 && ContainsKeyValue() == true)
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
                        Int32Rect faceRect = new Int32Rect((Math.Min(Math.Max((int)Math.Floor(positionOfHead[0]) - 100, 0), this.colorBitmap.PixelWidth)), (Math.Min(Math.Max((int)Math.Floor(positionOfHead[1]) - 100, 0), this.colorBitmap.PixelHeight)), 200, 200);
                        image.CopyPixels(faceRect, this.colorPixels, this.colorBitmap.PixelWidth * sizeof(int), 0);

                        BitmapSource image2 = BitmapSource.Create(200, 200, 96, 96, PixelFormats.Bgr32, null, this.colorPixels, this.colorBitmap.PixelWidth * sizeof(int));

                        reident.FacePhoto.Source = image2;

                        SaveImage(image2);
                        SendPictureToAPI(_face);

                        counti = counti + 1;
                    
                }
            }
        }

        /*
         * Function that handles the event if a body leaves the camera view and then calls startComparison call 
         * to send the body's face data to the comparer, and then remove face data from list of Faces in view
         * @param sender = kinect
         * @e Event fired if a body has left the camera view
         */
        private void HandleBodyLeftViewEvent(object sender, LeftViewEventArgs e)
        {

            FaceProcessor_face face = facesToProcess.Find(element => element.TrackingId == e.TrackingId);
            if (face != null)
            {
                reident.startComparison(face.TrackingId, face);
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


    /*
     * Class for comparison, Face Data will be stored in here.
     */
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
