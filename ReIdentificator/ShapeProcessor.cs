using System;
using System.Collections.Generic;
using Microsoft.Kinect;

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
        public ShapeProcessor(MainWindow mainWindow, KinectSensor kinect, Comparer comparer)
        {
            this.kinect = kinect;
            this.comparer = comparer;
            this.mainWindow = mainWindow;
            this.bodies = new Body[this.kinect.BodyFrameSource.BodyCount];
        }

        public void processBodyIndexFrame(BodyIndexFrame bodyIndexFrame, DepthFrame depthFrame, BodyFrame bodyFrame)
        {

            byte[] shapeToBeDrawn = new byte[depthFrame.FrameDescription.Width * depthFrame.FrameDescription.Height];
            for (int j = 0; j < shapeToBeDrawn.Length; j++)
            {
                shapeToBeDrawn[j] = 144;
            }

            bodyFrame.GetAndRefreshBodyData(this.bodies);

            bodyIndexFrameData =
              new byte[bodyIndexFrame.FrameDescription.Width *
                   bodyIndexFrame.FrameDescription.Height];
            bodyIndexFrame.CopyFrameDataToArray(bodyIndexFrameData);

            depthFrameData =
            new UInt16[depthFrame.FrameDescription.Width *
                 depthFrame.FrameDescription.Height];
            depthFrame.CopyFrameDataToArray(depthFrameData);

            List<CameraSpacePoint>[] cameraSpacePointsOfBodieCountour = new List<CameraSpacePoint>[this.kinect.BodyFrameSource.BodyCount];
            for (int i = 0; i < this.kinect.BodyFrameSource.BodyCount; i++)
            {
                cameraSpacePointsOfBodieCountour[i] = new List<CameraSpacePoint>();
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
                        CameraSpacePoint c = kinect.CoordinateMapper.MapDepthPointToCameraSpace(d, depthFrameData[j]);
                        cameraSpacePointsOfBodieCountour[j].Add(c);

                        DepthSpacePoint d2 = new DepthSpacePoint();
                        d.X = maxXForRow[j];
                        d.Y = y;
                        CameraSpacePoint c2 = kinect.CoordinateMapper.MapDepthPointToCameraSpace(d2, depthFrameData[j]);
                        cameraSpacePointsOfBodieCountour[j].Add(c2);
                    }

                }
            }
            mainWindow.RenderPixelArray(shapeToBeDrawn);
            processCameraSpacePointsOfCurrentFrame(cameraSpacePointsOfBodieCountour, depthFrame);
            // TestAusgabe:
            /*if (cameraSpacePointsOfBody1.Count > 0)
            {
                for (int i = 0; i < cameraSpacePointsOfBody1.Count; i++)
                {
                    CameraSpacePoint item = cameraSpacePointsOfBody1[i];

                    Debug.WriteLine(item.Z.ToString());
                }

            }*/

        }
        private void processCameraSpacePointsOfCurrentFrame(List<CameraSpacePoint>[] cameraSpacePointsOfBodies, DepthFrame depthFrame)
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
                    //TODO save in shape object meaningfull data
                }
            }

        }
    }
    class ShapeProcessor_shape
    {
        public ulong TrackingId { get; set; }
        public ShapeProcessor_shape(ulong trackingId)
        {
            this.TrackingId = trackingId;
        }


    }
}
