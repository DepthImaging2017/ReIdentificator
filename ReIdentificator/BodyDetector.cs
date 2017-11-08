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
        public int minimumDetectionPerBody = 3;
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
                        if (body.Height(3) > 0)
                            _body.heights.Add(body.Height(3));
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
                    if (!visibleInFrame) {
                        bodiesToProcess.RemoveAt(i);
                        processBody(b);
                    }
                }
            }
        }
        private void processBody(BodyDetector_body _body)
        {
            //TODO: ignore extremes?
            if (_body.heights.Count >= minimumDetectionPerBody)
            {
                _body.height = _body.heights.Average();
            }
            else
            {
                _body.height = -1;
            }

        }
    }
    class BodyDetector_body
    {
        public ulong TrackingId { get; set; }
        public double height { get; set; } // -1 if not detected properly
        public List<double> heights { get; set; } = new List<double>();

        //TODO
        public double torsoheight { get; set; }
        public List<double> torsoHeights { get; set; } = new List<double>();
        
        public BodyDetector_body(ulong trackingId)
        {
            this.TrackingId = trackingId;
        }


    }
}
