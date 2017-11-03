using System.Diagnostics;
using Microsoft.Kinect;

namespace ReIdentificator
{
    class BodyDetector
    {
        private BodyFrameReader bodyFrameReader = null;
        private Body[] bodies = null;

        private KinectSensor kinect;
        private Comparer comparer;
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
            bool hasTrackedBody = false;
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
                        
                        Debug.WriteLine("body in view with id: " + body.TrackingId);                       
                        hasTrackedBody = true;
                    }                  
                }

                if (!hasTrackedBody)
                {
                    // Do something when no bodies are there
                }
            }
        }
    }
}
