using System;
using System.Collections.Generic;
using Microsoft.Kinect;

namespace ReIdentificator
{
    public static class Util
    {
        /// <param name="percent">The percentage to drop from each side.</param>
        public static double trimmedMean(List<Double> values, double percent)
        {
            values.Sort();
            int k = (int)Math.Floor(values.Count * percent);

            double sum = 0;
            for (int i = k; i < values.Count - k; i++)
                sum += values[i];
            return sum / (values.Count - 2 * k);

        }
        public static double distanceBetweenSpacePoints(CameraSpacePoint p1, CameraSpacePoint p2)
        {
            return Math.Sqrt(
                Math.Pow(p1.X - p2.X, 2) +
                Math.Pow(p1.Y - p2.Y, 2) +
                Math.Pow(p1.Z - p2.Z, 2));
        }
        public static void MergeWith<T>(this T primary, T secondary)
        {
            foreach (var pi in typeof(T).GetProperties())
            {
                var priValue = pi.GetGetMethod().Invoke(primary, null);
                var secValue = pi.GetGetMethod().Invoke(secondary, null);
                if (priValue == null || (pi.PropertyType.IsValueType && priValue.Equals(Activator.CreateInstance(pi.PropertyType))))
                {
                    pi.GetSetMethod().Invoke(primary, new object[] { secValue });
                }
            }
        }
    }
}
