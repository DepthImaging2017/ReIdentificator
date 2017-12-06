using System;
using System.Collections.Generic;


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
    }
}
