using System;
using System.Collections.Generic;
using Microsoft.Kinect;
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
        public ShapeProcessor(MainWindow mainWindow, KinectSensor kinect, Comparer comparer)
        {
            this.kinect = kinect;
            this.comparer = comparer;
            this.mainWindow = mainWindow;

        }

        public void processBodyIndexFrame(BodyIndexFrame bodyIndexFrame, DepthFrame depthFrame)
        {

            bodyIndexFrameData =
              new byte[bodyIndexFrame.FrameDescription.Width *
                   bodyIndexFrame.FrameDescription.Height];
            bodyIndexFrame.CopyFrameDataToArray(bodyIndexFrameData);

            depthFrameData =
            new UInt16[depthFrame.FrameDescription.Width *
                 depthFrame.FrameDescription.Height];
            depthFrame.CopyFrameDataToArray(depthFrameData);
            List<CameraSpacePoint> cameraSpacePointsOfShape = new List<CameraSpacePoint>();

            for (int y = 0; y < depthFrame.FrameDescription.Height; y++)
            {
                for (int x = 0; x < depthFrame.FrameDescription.Width; x++)
                {
                    int depthIndex = (y * depthFrame.FrameDescription.Width) + x;
                    byte pixelValue = bodyIndexFrameData[depthIndex];
                    if (pixelValue < 255)
                    {
                        DepthSpacePoint d = new DepthSpacePoint();
                        d.X = x;
                        d.Y = y;
                        CameraSpacePoint c = kinect.CoordinateMapper.MapDepthPointToCameraSpace(d, depthFrameData[depthIndex]);
                        cameraSpacePointsOfShape.Add(c);
                    }
                }


            }
            // TestAusgabe:
            if (cameraSpacePointsOfShape.Count > 0)
            {
                for (int i = 0; i < cameraSpacePointsOfShape.Count; i++)
                {
                    CameraSpacePoint item = cameraSpacePointsOfShape[i];

                    Debug.WriteLine(item.Z.ToString());
                }

            }

        }
    }
}
