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
        private int minimumDetectionPerBody = 10;
        private int minimumLegJointsDetected = 3;
        private Microsoft.Kinect.Vector4 clipPlane;
        public BodyDetector(KinectSensor kinect, Comparer comparer)
        {
            this.kinect = kinect;
            this.comparer = comparer;
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
            //if (body.Height(minimumLegJointsDetected) > 0)
            //_body.heights.Add(body.Height(minimumLegJointsDetected));
            _body.heights.Add(body.HeightOfBody(clipPlane));
            _body.torsoHeights.Add(body.UpperHeight());
            _body.neckToSpineMid_list.Add(body.DistanceBetweenTwoJoints(JointType.Neck, JointType.SpineMid));
            _body.spineMidToSpineBase_list.Add(body.DistanceBetweenTwoJoints(JointType.SpineMid, JointType.SpineBase));
            _body.neckToLeftShoulder_list.Add(body.DistanceBetweenTwoJoints(JointType.Neck, JointType.ShoulderLeft));
            _body.neckToRightShoulder_list.Add(body.DistanceBetweenTwoJoints(JointType.Neck, JointType.ShoulderRight));
            _body.leftHipToSpineBase_list.Add(body.DistanceBetweenTwoJoints(JointType.HipLeft, JointType.SpineBase));
            _body.rightHipToSpineBase_list.Add(body.DistanceBetweenTwoJoints(JointType.HipRight, JointType.SpineBase));
            _body.spineMidToLeftShoulder_list.Add(body.DistanceBetweenTwoJoints(JointType.SpineMid, JointType.ShoulderLeft));
            _body.spineMidToRightShoulder_list.Add(body.DistanceBetweenTwoJoints(JointType.SpineMid, JointType.ShoulderLeft));

        }
        private void processBody(BodyDetector_body _body)
        {
            //TODO: ignore extremes?
            if (_body.heights.Count >= minimumDetectionPerBody)
            {
                _body.height = _body.heights.Average();
            }
            if (_body.torsoHeights.Count >= minimumDetectionPerBody)
            {
                _body.torsoHeight = _body.torsoHeights.Average();
                _body.neckToSpineMid = _body.neckToSpineMid_list.Average();
                _body.spineMidToSpineBase = _body.spineMidToSpineBase_list.Average();
                _body.neckToLeftShoulder = _body.neckToLeftShoulder_list.Average();
                _body.neckToRightShoulder = _body.neckToRightShoulder_list.Average();
                _body.leftHipToSpineBase = _body.leftHipToSpineBase_list.Average();
                _body.rightHipToSpineBase = _body.rightHipToSpineBase_list.Average();
                _body.spineMidToLeftShoulder = _body.spineMidToLeftShoulder_list.Average();
                _body.spineMidToRightShoulder = _body.spineMidToRightShoulder_list.Average();
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
