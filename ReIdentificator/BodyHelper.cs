using System;
using Microsoft.Kinect;
using System.Diagnostics;
using System.Numerics;

public static class BodyExtension
{

    /*
     * TODO: To be corrected!!
     */
    public static double HeightOfBody(this Body TargetBody, Microsoft.Kinect.Vector4 clipPlane)
    {
        double height = clipPlane.W;
        if (Math.Abs(height) > double.Epsilon)
        {
            CameraSpacePoint head = TargetBody.Joints[JointType.Head].Position;
            double result = DistanceToPlane(clipPlane, head);
            return result;

        }
        else
        {
            return -1;
        }

    }
    public static double DistanceBetweenTwoJoints(this Body TargetBody, JointType typeA, JointType typeB)
    {
        Joint typeA_Joint = TargetBody.Joints[typeA];
        Joint typeB_Joint = TargetBody.Joints[typeB];
        if (NumberOfTrackedJoints(typeA_Joint, typeB_Joint) == 2)
        {
            return Length(typeA_Joint, typeB_Joint);
        }
        return -1;

    }
    private static double DistanceToPlane(Microsoft.Kinect.Vector4 clipPlane, CameraSpacePoint point)
    {
        double X = clipPlane.X;
        double Y = clipPlane.Y;
        double Z = clipPlane.Z;
        double W = clipPlane.W;

        double numerator = X * point.X + Y * point.Y + Z * point.Z + W;
        double denominator = Math.Sqrt(X * X + Y * Y + Z * Z);
        return numerator / denominator;

    }
    public static double UpperHeight(this Body TargetBody)
    {
        Joint _head = TargetBody.Joints[JointType.Head];
        Joint _neck = TargetBody.Joints[JointType.SpineMid];
        Joint _spine = TargetBody.Joints[JointType.SpineShoulder];
        Joint _waist = TargetBody.Joints[JointType.SpineBase];
        return Length(_head, _neck, _spine, _waist);
    }
    public static double Length(Joint p1, Joint p2)
    {
        return Math.Sqrt(
            Math.Pow(p1.Position.X - p2.Position.X, 2) +
            Math.Pow(p1.Position.Y - p2.Position.Y, 2) +
            Math.Pow(p1.Position.Z - p2.Position.Z, 2));
    }
    public static double Length(params Joint[] joints)
    {
        double length = 0;

        for (int index = 0; index < joints.Length - 1; index++)
        {
            length += Length(joints[index], joints[index + 1]);
        }

        return length;
    }
    // Given a collection of joints, calculates the number of the joints that are tracked accurately.
    public static int NumberOfTrackedJoints(params Joint[] joints)
    {
        int trackedJoints = 0;
        foreach (var joint in joints)
        {
            if (joint.TrackingState == TrackingState.Tracked)
            {
                trackedJoints++;
            }
        }
        return trackedJoints;
    }
    public static double Yaw(this Microsoft.Kinect.Vector4 quaternion)
    {
        double value = 2.0 * (quaternion.W * quaternion.Y - quaternion.Z * quaternion.X);
        value = value > 1.0 ? 1.0 : value;
        value = value < -1.0 ? -1.0 : value;

        double pitch = Math.Asin(value);

        return pitch * (180.0 / Math.PI);
    }
}

