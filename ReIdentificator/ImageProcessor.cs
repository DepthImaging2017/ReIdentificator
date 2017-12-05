﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ReIdentificator
{
    class ImageProcessor
    {
        private KinectSensor kinect;
        private Comparer comparer;
        private ColorFrameReader colorFrameReader = null;
        private byte[] colorPixels;
        private WriteableBitmap colorBitmap;
        private MainWindow mainWindow;
        public ImageProcessor(KinectSensor kinect, Comparer comparer, MainWindow mainWindow)
        {
            this.kinect = kinect;
            this.comparer = comparer;
            this.mainWindow = mainWindow;

            this.colorFrameReader = this.kinect.ColorFrameSource.OpenReader();
            this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;
        }

        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    this.colorPixels = new byte[this.kinect.ColorFrameSource.FrameDescription.LengthInPixels * 4];
                    colorFrame.CopyConvertedFrameDataToArray(this.colorPixels, Microsoft.Kinect.ColorImageFormat.Bgra);
                    this.colorBitmap = new WriteableBitmap(this.kinect.ColorFrameSource.FrameDescription.Width, this.kinect.ColorFrameSource.FrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

                    // Write the pixel data into our bitmap
                    this.colorBitmap.WritePixels(
                        new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                        this.colorPixels,
                        this.colorBitmap.PixelWidth * sizeof(int),
                        0);

                    //mainWindow.FrameDisplayImage.Source = this.colorBitmap;

                }
            }
        }

        public int AvgOf2MainColors(int color1, int color2)
        {
            int avgMainColor = (int)Math.Sqrt((Math.Pow(color1, 2) + Math.Pow(color2, 2)) / 2);
            return avgMainColor;
        }

        public int AvgOfXMainColors(int[] colors)
        {
            int colorTemp = 0;
            for(int i = 0; i<colors.Length; i++)
            {
                colorTemp += (int) Math.Pow(colors[i],2);
            }
            colorTemp = (int) Math.Sqrt(colorTemp / colors.Length);

            return colorTemp;
        }

        public int[] AvgColor(int r, int g, int b)
         {
             int[] color = new int[3] { r, g, b };
             return color;
         }

         /* public int IndicatorColor(int r1, int g1, int b1, int avgr2, int avgg2, int avgb2)
         {
             int newR = AvgOf2MainColors(r1, avgr2);
             int newG = AvgOf2MainColors(g1, avgg2);
             int newB = AvgOf2MainColors(b1, avgb2);
             return (newR+ avgr2+ newG+ avgg2 + newB+ avgb2) /3;
         }

         public PostAvgColor(int[] colors, int preColor)
         {
             int[] indicatorsError;
             int[] postErrorColors;
             int j = 0;

             int error = 0;

             for (int i = 0; i < colors.Length; i++)
             {
                 indicatorsError[i] = IndicatorColor(colors[0.r], colors[0.g], colors[0.b], preColor.r, preColor.b, preColor.g);
                 error += indicatorsError[i];
             }
             error = error / indicatorsError.Length;
             for (int i = 0; i < indicatorsError.Length; i++)
             {
                 if(indicatorsError[i] < error)
                 {
                     j++;
                     postErrorColors[j] = colors[i];
                 }
             }

             Color finalColor = postErrorColors[0];
             for (int i = 1; i < postErrorColors.Length; i ++)
             {
                 arrCol = postErrorColors[i]
                 finalColor = AvgColor(arrCol.r, arrCol.g, arrCol.b, finalColor.r, finalColor.g, finalColor.b);
             }
             return finalColor;
         }*/
    }
}
