using System;
using System.Collections.Generic;
using Microsoft.Kinect;
using System.Linq;
using System.Diagnostics;

namespace ReIdentificator
{
    class ShapeProcessor
    {
        private Comparer comparer;
        private MainWindow mainWindow;
        private KinectSensor kinect;
        private byte[] bodyIndexFrameData = null;
        private UInt16[] depthFrameData = null;
        private Body[] bodies = null;
        private List<ShapeProcessor_shape> shapesToProcess = new List<ShapeProcessor_shape>();
        private readonly int minimumDetectionPerBody = 4;
        private readonly double minDistanceToSensorPlane = 0.8;
        private readonly double maxDistanceToSensorPlane = 4;
        public ShapeProcessor(MainWindow mainWindow, KinectSensor kinect, Comparer comparer)
        {
            this.kinect = kinect;
            this.comparer = comparer;
            this.mainWindow = mainWindow;
            this.bodies = new Body[this.kinect.BodyFrameSource.BodyCount];
            mainWindow.BodyLeftView += HandleBodyLeftViewEvent;
        }

        public void processBodyIndexFrame(BodyIndexFrame bodyIndexFrame, DepthFrame depthFrame, BodyFrame bodyFrame)
        {
            byte[] shapeToBeDrawn = GetEmptyByteArrayForDrawing(depthFrame);

            bodyFrame.GetAndRefreshBodyData(this.bodies);

            bodyIndexFrameData =
              new byte[bodyIndexFrame.FrameDescription.Width *
                   bodyIndexFrame.FrameDescription.Height];
            bodyIndexFrame.CopyFrameDataToArray(bodyIndexFrameData);

            depthFrameData =
            new UInt16[depthFrame.FrameDescription.Width *
                 depthFrame.FrameDescription.Height];
            depthFrame.CopyFrameDataToArray(depthFrameData);

            List<CameraSpacePoint>[] cameraSpacePointsOfBodyCountour = new List<CameraSpacePoint>[this.kinect.BodyFrameSource.BodyCount];
            for (int i = 0; i < this.kinect.BodyFrameSource.BodyCount; i++)
            {
                cameraSpacePointsOfBodyCountour[i] = new List<CameraSpacePoint>();
            }

            for (int y = 0; y < depthFrame.FrameDescription.Height; y++)
            {
                int[] minXForRow = new int[kinect.BodyFrameSource.BodyCount];
                int[] maxXForRow = new int[kinect.BodyFrameSource.BodyCount];
                for (int i = 0; i < minXForRow.Length; i++)
                {
                    minXForRow[i] = depthFrame.FrameDescription.Width + 1;
                }
                for (int i = 0; i < minXForRow.Length; i++)
                {
                    maxXForRow[i] = -1;
                }
                for (int x = 0; x < depthFrame.FrameDescription.Width; x++)
                {
                    int depthIndex = (y * depthFrame.FrameDescription.Width) + x;
                    byte pixelValue = bodyIndexFrameData[depthIndex];
                    if (pixelValue < 255)
                    {
                        if (x < minXForRow[pixelValue])
                        {
                            minXForRow[pixelValue] = x;
                        }
                        if (x > maxXForRow[pixelValue])
                        {
                            maxXForRow[pixelValue] = x;
                        }
                    }
                }

                for (int j = 0; j < minXForRow.Length; j++)
                {
                    if (minXForRow[j] < depthFrame.FrameDescription.Width + 1 && maxXForRow[j] > -1)
                    {
                        shapeToBeDrawn[minXForRow[j] + y * depthFrame.FrameDescription.Width] = 33;
                        shapeToBeDrawn[maxXForRow[j] + y * depthFrame.FrameDescription.Width] = 33;

                        DepthSpacePoint d = new DepthSpacePoint();
                        d.X = minXForRow[j];
                        d.Y = y;
                        CameraSpacePoint c = kinect.CoordinateMapper.MapDepthPointToCameraSpace(d, depthFrameData[minXForRow[j] + y * depthFrame.FrameDescription.Width]);
                        if (!float.IsInfinity(c.X) && !float.IsInfinity(c.Y) && !float.IsInfinity(c.Z))
                            cameraSpacePointsOfBodyCountour[j].Add(c);

                        DepthSpacePoint d2 = new DepthSpacePoint();
                        d2.X = maxXForRow[j];
                        d2.Y = y;
                        CameraSpacePoint c2 = kinect.CoordinateMapper.MapDepthPointToCameraSpace(d2, depthFrameData[maxXForRow[j] + y * depthFrame.FrameDescription.Width]);

                        if (!float.IsInfinity(c2.X) && !float.IsInfinity(c2.Y) && !float.IsInfinity(c2.Z))
                            cameraSpacePointsOfBodyCountour[j].Add(c2);

                    }

                }
            }
            processCameraSpacePointsOfCurrentFrame(cameraSpacePointsOfBodyCountour, depthFrame, shapeToBeDrawn);

        }
        private void processCameraSpacePointsOfCurrentFrame(List<CameraSpacePoint>[] cameraSpacePointsOfBodies, DepthFrame depthFrame, byte[] shapeToBeDrawn)
        {
            for (int i = 0; i < cameraSpacePointsOfBodies.Length; i++)
            {

                if (cameraSpacePointsOfBodies[i].Count > 0)
                {

                    ulong TrackingId = bodies[i].TrackingId;
                    if (!shapesToProcess.Exists(element => element.TrackingId == TrackingId))
                    {
                        shapesToProcess.Add(new ShapeProcessor_shape(TrackingId));
                    }
                    ShapeProcessor_shape shape = shapesToProcess.Find(element => element.TrackingId == TrackingId);
                    if (Math.Abs(bodies[i].JointOrientations[JointType.SpineMid].Orientation.Yaw()) < 22
                    && bodies[i].Joints[JointType.SpineMid].Position.Z > minDistanceToSensorPlane && bodies[i].Joints[JointType.SpineMid].Position.Z < maxDistanceToSensorPlane)
                    {
                        CameraSpacePoint shoulderLeftPoint = new CameraSpacePoint();
                        double minLeftDistance = 1000;
                        CameraSpacePoint shoulderRightPoint = new CameraSpacePoint();
                        double minRightDistance = 1000;
                        for (int k = 0; k < cameraSpacePointsOfBodies[i].Count; k++)
                        {
                            CameraSpacePoint currentSpacePoint = cameraSpacePointsOfBodies[i][k];
                            double leftDistance = Util.distanceBetweenSpacePoints(currentSpacePoint, bodies[i].Joints[JointType.ShoulderLeft].Position);
                            if (leftDistance < minLeftDistance)
                            {
                                minLeftDistance = leftDistance;
                                shoulderLeftPoint = currentSpacePoint;
                            }

                            double rightDistance = Util.distanceBetweenSpacePoints(currentSpacePoint, bodies[i].Joints[JointType.ShoulderRight].Position);
                            if (rightDistance < minRightDistance)
                            {
                                minRightDistance = rightDistance;
                                shoulderRightPoint = currentSpacePoint;
                            }
                        }

                        double bodywidth = Util.distanceBetweenSpacePoints(shoulderRightPoint, shoulderLeftPoint);
                        if (bodywidth > 0)
                            shape.bodyWidth_list.Add(bodywidth);
                        /*
                        *  for drawing:
                        */
                        var firstShoulderDepthPoint = kinect.CoordinateMapper.MapCameraPointToDepthSpace(shoulderLeftPoint);
                        var secondShoulderDepthPoint = kinect.CoordinateMapper.MapCameraPointToDepthSpace(shoulderRightPoint);
                        for (int j = 0; j < 10; j++)
                        {
                            shapeToBeDrawn[(int)firstShoulderDepthPoint.X + j + (int)firstShoulderDepthPoint.Y * depthFrame.FrameDescription.Width] = 100;
                            shapeToBeDrawn[(int)secondShoulderDepthPoint.X + j + (int)secondShoulderDepthPoint.Y * depthFrame.FrameDescription.Width] = 33;
                        }
                        mainWindow.RenderPixelArray(shapeToBeDrawn, mainWindow.FrameDisplayImage);



                    }
                }
            }

        }
        private void HandleBodyLeftViewEvent(object sender, LeftViewEventArgs e)
        {
            double trimmedMeanPercentage = 0.2;

            ShapeProcessor_shape shape = shapesToProcess.Find(element => element.TrackingId == e.TrackingId);
            if (shape != null && shape.bodyWidth_list.Count >= minimumDetectionPerBody)
            {
                shape.bodyWidth = Util.trimmedMean(shape.bodyWidth_list, trimmedMeanPercentage);
                //mainWindow.printLog("Body width: " + shape.bodyWidth);
                mainWindow.startComparison(shape.TrackingId, shape);
            }
            shapesToProcess.Remove(shape);
        }
        private byte[] GetEmptyByteArrayForDrawing(DepthFrame depthFrame)
        {
            byte[] emptyByteArray = new byte[depthFrame.FrameDescription.Width * depthFrame.FrameDescription.Height];
            for (int j = 0; j < emptyByteArray.Length; j++)
            {
                emptyByteArray[j] = 144;
            }
            return emptyByteArray;

        }
    }

    class ShapeProcessor_shape
    {
        public ulong TrackingId { get; set; }
        public double bodyWidth { get; set; } // -1 if not detected properly
        public List<double> bodyWidth_list { get; set; } = new List<double>();
        public ShapeProcessor_shape(ulong trackingId)
        {
            this.TrackingId = trackingId;
        }


    }
}
