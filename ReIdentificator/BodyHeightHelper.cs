// CREDITS
// partly based on https://pterneas.com/2012/06/08/kinect-for-windows-find-user-height-accurately/
// and https://github.com/jhealy/kinect2
//
// MIT License
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associateddocumentation files (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY

using System;
using Microsoft.Kinect;

public static class BodyHeightExtension
{

    public static double Height(this Body TargetBody, int minLegJoints)
    {
        if (TargetBody == null) return -1.0;
        if (TargetBody.IsTracked == false) return -2.0;

        const double HEAD_DIVERGENCE = 0.1;

        Joint _head = TargetBody.Joints[JointType.Head];
        Joint _neck = TargetBody.Joints[JointType.Neck];

        Joint _spine = TargetBody.Joints[JointType.SpineShoulder];
        Joint _waist = TargetBody.Joints[JointType.SpineBase];
        Joint _hipLeft = TargetBody.Joints[JointType.HipLeft];
        Joint _hipRight = TargetBody.Joints[JointType.HipRight];
        Joint _kneeLeft = TargetBody.Joints[JointType.KneeLeft];
        Joint _kneeRight = TargetBody.Joints[JointType.KneeRight];
        Joint _ankleLeft = TargetBody.Joints[JointType.AnkleLeft];
        Joint _ankleRight = TargetBody.Joints[JointType.AnkleRight];
        Joint _footLeft = TargetBody.Joints[JointType.FootLeft];
        Joint _footRight = TargetBody.Joints[JointType.FootRight];

        // Find which leg is tracked more accurately.
        int legLeftTrackedJoints = NumberOfTrackedJoints(_hipLeft, _kneeLeft, _ankleLeft, _footLeft);
        int legRightTrackedJoints = NumberOfTrackedJoints(_hipRight, _kneeRight, _ankleRight, _footRight);

        if (legLeftTrackedJoints < minLegJoints && legRightTrackedJoints < minLegJoints)
        {
            return -3;
        }


        double legLength = legLeftTrackedJoints > legRightTrackedJoints ? Length(_hipLeft, _kneeLeft, _ankleLeft, _footLeft)
            : Length(_hipRight, _kneeRight, _ankleRight, _footRight);
        double _retval = Length(_head, _neck, _spine, _waist) + legLength + HEAD_DIVERGENCE;

        return _retval;
    }

    /// <summary>
    /// Returns the upper height of the specified skeleton (head to waist).
    /// </summary>
    public static double UpperHeight(this Body TargetBody)
    {
        Joint _head = TargetBody.Joints[JointType.Head];        
        Joint _neck = TargetBody.Joints[JointType.SpineMid];        
        Joint _spine = TargetBody.Joints[JointType.SpineShoulder];        
        Joint _waist = TargetBody.Joints[JointType.SpineBase];
        return Length(_head, _neck, _spine, _waist);
    }

    /// <summary>
    /// Returns the length of the segment defined by the specified joints.
    /// </summary>
    /// <param name="p1">The first joint (start of the segment).</param>
    /// <param name="p2">The second joint (end of the segment).</param>
    /// <returns>The length of the segment in meters.</returns>
    public static double Length(Joint p1, Joint p2)
    {
        return Math.Sqrt(
            Math.Pow(p1.Position.X - p2.Position.X, 2) +
            Math.Pow(p1.Position.Y - p2.Position.Y, 2) +
            Math.Pow(p1.Position.Z - p2.Position.Z, 2));
    }

    /// <summary>
    /// Returns the length of the segments defined by the specified joints.
    /// </summary>
    /// <param name="joints">A collection of two or more joints.</param>
    /// <returns>The length of all the segments in meters.</returns>
    public static double Length(params Joint[] joints)
    {
        double length = 0;

        for (int index = 0; index < joints.Length - 1; index++)
        {
            length += Length(joints[index], joints[index + 1]);
        }

        return length;
    }

    /// <summary>
    /// Given a collection of joints, calculates the number of the joints that are tracked accurately.
    /// </summary>
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
}
