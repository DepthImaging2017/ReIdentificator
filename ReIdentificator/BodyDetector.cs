using System.Diagnostics;
using Microsoft.Kinect;
using System.Collections.Generic;
using System.Linq;

namespace ReIdentificator
{
    class BodyDetector
    {
        private BodyFrameReader bodyFrameReader = null;
        private Body[] bodies = null;
        private List<BodyDetector_body> bodiesToProcess = new List<BodyDetector_body>();
        private KinectSensor kinect;
        private Comparer comparer;
        private MainWindow UI;
        private Microsoft.Kinect.Vector4 clipPlane;

        private readonly int minimumDetectionPerBody = 10;
        private readonly double minDistanceToSensorPlane = 0.8;
        private readonly double maxDistanceToSensorPlane = 4;

        public BodyDetector(MainWindow ui, KinectSensor kinect, Comparer comparer)
        {
            this.kinect = kinect;
            this.comparer = comparer;
            this.UI = ui;
            this.bodyFrameReader = this.kinect.BodyFrameSource.OpenReader();
            this.bodyFrameReader.FrameArrived += this.Reader_BodyFrameArrived;
            this.bodies = new Body[this.kinect.BodyFrameSource.BodyCount];
        }
        private void Reader_BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    clipPlane = bodyFrame.FloorClipPlane;
                    dataReceived = true;

                }
            }

            if (dataReceived)
            {

                // iterate through each body
                for (int bodyIndex = 0; bodyIndex < this.bodies.Length; bodyIndex++)
                {
                    Body body = this.bodies[bodyIndex];
                    if (body.IsTracked)
                    {
                        if (!bodiesToProcess.Exists(element => element.TrackingId == body.TrackingId))
                        {
                            bodiesToProcess.Add(new BodyDetector_body(body.TrackingId));
                        }
                        BodyDetector_body _body = bodiesToProcess.Find(element => element.TrackingId == body.TrackingId);
                        calculateBodyDataForCurrentFrame(_body, body);

                    }

                }
                // detect if body has left frame, then process it
                for (int i = 0; i < this.bodiesToProcess.Count; i++)
                {
                    BodyDetector_body b = bodiesToProcess[i];
                    bool visibleInFrame = false;
                    for (int bodyIndex = 0; bodyIndex < this.bodies.Length; bodyIndex++)
                    {
                        Body body = this.bodies[bodyIndex];
                        if (body.TrackingId == b.TrackingId)
                            visibleInFrame = true;

                    }
                    // body left frame
                    if (!visibleInFrame)
                    {
                        bodiesToProcess.RemoveAt(i);
                        processBody(b);
                    }
                }
            }
        }
        private void calculateBodyDataForCurrentFrame(BodyDetector_body _body, Body body)
        {
            //don't add if person is walking sideway or if outside determinated range
            if (System.Math.Abs(body.JointOrientations[JointType.SpineMid].Orientation.Yaw()) < 22
                && body.Joints[JointType.SpineMid].Position.Z > minDistanceToSensorPlane && body.Joints[JointType.SpineMid].Position.Z < maxDistanceToSensorPlane)
            {
                double valueToAdd = -1;
                valueToAdd = body.HeightOfBody(clipPlane);
                if (valueToAdd > 0)
                    _body.heights.Add(valueToAdd);
                valueToAdd = body.UpperHeight();
                if (valueToAdd > 0)
                    _body.torsoHeights.Add(valueToAdd);
                valueToAdd = body.DistanceBetweenTwoJoints(JointType.Neck, JointType.SpineMid);
                if (valueToAdd > 0)
                    _body.neckToSpineMid_list.Add(valueToAdd);
                valueToAdd = body.DistanceBetweenTwoJoints(JointType.SpineMid, JointType.SpineBase);
                if (valueToAdd > 0)
                    _body.spineMidToSpineBase_list.Add(valueToAdd);
                valueToAdd = body.DistanceBetweenTwoJoints(JointType.Neck, JointType.ShoulderLeft);
                if (valueToAdd > 0)
                    _body.neckToLeftShoulder_list.Add(valueToAdd);
                valueToAdd = body.DistanceBetweenTwoJoints(JointType.Neck, JointType.ShoulderRight);
                if (valueToAdd > 0)
                    _body.neckToRightShoulder_list.Add(valueToAdd);
                valueToAdd = body.DistanceBetweenTwoJoints(JointType.HipLeft, JointType.SpineBase);
                if (valueToAdd > 0)
                    _body.leftHipToSpineBase_list.Add(valueToAdd);
                valueToAdd = body.DistanceBetweenTwoJoints(JointType.HipRight, JointType.SpineBase);
                if (valueToAdd > 0)
                    _body.rightHipToSpineBase_list.Add(valueToAdd);
                valueToAdd = body.DistanceBetweenTwoJoints(JointType.SpineMid, JointType.ShoulderLeft);
                if (valueToAdd > 0)
                    _body.spineMidToLeftShoulder_list.Add(valueToAdd);
                valueToAdd = body.DistanceBetweenTwoJoints(JointType.SpineMid, JointType.ShoulderLeft);
                if (valueToAdd > 0)
                    _body.spineMidToRightShoulder_list.Add(valueToAdd);
            }

        }
        private void processBody(BodyDetector_body _body)
        {
            double trimmedMeanPercentage = 0.2;
            if (_body.heights.Count >= minimumDetectionPerBody)
            {
                _body.height = _body.heights.Average();
            }
            if (_body.torsoHeights.Count >= minimumDetectionPerBody)
            {
                _body.torsoHeight = Util.trimmedMean(_body.torsoHeights, trimmedMeanPercentage);
                _body.neckToSpineMid = Util.trimmedMean(_body.neckToSpineMid_list, trimmedMeanPercentage);
                _body.spineMidToSpineBase = Util.trimmedMean(_body.spineMidToSpineBase_list, trimmedMeanPercentage);
                _body.neckToLeftShoulder = Util.trimmedMean(_body.neckToLeftShoulder_list, trimmedMeanPercentage);
                _body.neckToRightShoulder = Util.trimmedMean(_body.neckToRightShoulder_list, trimmedMeanPercentage);
                _body.leftHipToSpineBase = Util.trimmedMean(_body.leftHipToSpineBase_list, trimmedMeanPercentage);
                _body.rightHipToSpineBase = Util.trimmedMean(_body.rightHipToSpineBase_list, trimmedMeanPercentage);
                _body.spineMidToLeftShoulder = Util.trimmedMean(_body.spineMidToLeftShoulder_list, trimmedMeanPercentage);
                _body.spineMidToRightShoulder = Util.trimmedMean(_body.spineMidToRightShoulder_list, trimmedMeanPercentage);
                UI.printLog("body parameters: " + _body.neckToSpineMid + " " + _body.spineMidToSpineBase + " " + _body.neckToLeftShoulder + " " + _body.leftHipToSpineBase + " " + _body.spineMidToLeftShoulder);
            }
        }
    }
    class BodyDetector_body
    {
        public ulong TrackingId { get; set; }
        public double height { get; set; } // -1 if not detected properly
        public List<double> heights { get; set; } = new List<double>();
        public double torsoHeight { get; set; }
        public List<double> torsoHeights { get; set; } = new List<double>();
        public double neckToSpineMid { get; set; }
        public List<double> neckToSpineMid_list { get; set; } = new List<double>();
        public double spineMidToSpineBase { get; set; }
        public List<double> spineMidToSpineBase_list { get; set; } = new List<double>();
        public double neckToLeftShoulder { get; set; }
        public List<double> neckToLeftShoulder_list { get; set; } = new List<double>();
        public double neckToRightShoulder { get; set; }
        public List<double> neckToRightShoulder_list { get; set; } = new List<double>();
        public double leftHipToSpineBase { get; set; }
        public List<double> leftHipToSpineBase_list { get; set; } = new List<double>();
        public double rightHipToSpineBase { get; set; }
        public List<double> rightHipToSpineBase_list { get; set; } = new List<double>();
        public double spineMidToLeftShoulder { get; set; }
        public List<double> spineMidToLeftShoulder_list { get; set; } = new List<double>();
        public double spineMidToRightShoulder { get; set; }
        public List<double> spineMidToRightShoulder_list { get; set; } = new List<double>();

        public BodyDetector_body(ulong trackingId)
        {
            this.TrackingId = trackingId;
        }


    }
}
