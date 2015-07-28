﻿using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace Wallpaperer.Droplet
{
    public class Program
    {
        private static readonly Int32 MaxWidth;
        private static readonly Int16 DisplayWidth;
        private static readonly Int16 DisplayHeight;
        private static readonly Byte BezelWidth;
        private static readonly Byte DisplayCount;

        /// <summary>
        /// Initializes the <see cref="Program"/> class.
        /// </summary>
        /// <remarks>Reads the settings from the settings configuration file.</remarks>
        static Program()
        {
            DisplayWidth = Settings.Default.DisplayWidth;
            DisplayHeight = Settings.Default.DisplayHeight;
            BezelWidth = Settings.Default.BezelWidth;
            DisplayCount = Settings.Default.DisplayCount;
            MaxWidth = (DisplayWidth * DisplayCount) + (BezelWidth * ((DisplayCount - 1)*2));
        }

        /// <summary>
        /// Takes a 6000x1080 bitmap and crops it to 5760x1080 while allowing for the bezel width of the monitors.
        /// </summary>
        /// <param name="args">The arguments. The path to a valid bitmap file.</param>
        /// <remarks>Writes a file of the same name with -wallpapered appended to the end in PNG format to the same location as the source.</remarks>
        public static void Main(String[] args)
        {
            if (args.Length != 1)
                DisplayInstructions();

            try
            {
                String filePath = args[0];
                String fileName = Path.GetFileNameWithoutExtension(filePath);
                String fileDirectory = Path.GetDirectoryName(filePath);
                String newFilePath = String.Format("{0}{1}{2}-wallpapered.png",fileDirectory,Path.DirectorySeparatorChar,fileName);                

                using (Bitmap sourceBitmap = new Bitmap(filePath))
                {
                    //Check size
                    if (sourceBitmap.Width != MaxWidth || sourceBitmap.Height != DisplayHeight)
                        DisplayInstructions();

                    //Wallpaper Bitmap
                    Bitmap wallpaper = new Bitmap(DisplayWidth * DisplayCount,DisplayHeight);

                    //Set up cropping region and bitmap
                    Size area = new Size(DisplayWidth, DisplayHeight);
                    Point sourceOrigin = new Point(0, 0);
                    Point destinationOrigin = new Point(0, 0);
                    Rectangle sourceRegion = new Rectangle(sourceOrigin, area);
                    Rectangle destinationRegion = new Rectangle(sourceOrigin,area);                    
                    //Bitmap cropped;

                    Int32 monitorWidth = area.Width + (BezelWidth * 2);
                    for (int i = 0; i < DisplayCount; i++)
                    {
                        //Move origins
                        sourceOrigin.X = monitorWidth * i;
                        sourceRegion.Location = sourceOrigin;
                        destinationOrigin.X = DisplayWidth * i;
                        destinationRegion.Location = destinationOrigin;
                        
                        //Perform crop
                        //cropped = Clone(sourceBitmap, sourceRegion);

                        //Add the cropped image to the result bitmap.
                        //CopyRegionIntoImage(cropped, ref wallpaper, destinationRegion);
                        CopyRegionIntoImage(sourceBitmap, sourceRegion, ref wallpaper, destinationRegion);
                    }

                    wallpaper.Save(newFilePath);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Clones the specified source.
        /// </summary>
        /// <param name="source">The source bitmap.</param>
        /// <param name="cropArea">The crop area.</param>
        /// <returns>The cropped bitmap.</returns>
        private static Bitmap Clone(Bitmap source, Rectangle cropArea)
        {
            Bitmap bitmap = new Bitmap(source);

            return source.Clone(cropArea, bitmap.PixelFormat);
        }

        /// <summary>
        /// Copies the region into image.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="destinationRegion">The destination region.</param>
        /// <remarks>Determines the source-region internally and is based upon a full bitmap being passed in.</remarks>
        private static void CopyRegionIntoImage(Bitmap source, ref Bitmap destination, Rectangle destinationRegion)
        {
            using(Graphics graphics = Graphics.FromImage(destination))
            {
                Rectangle sourceRegion = new Rectangle(0, 0, source.Width, source.Height);
                graphics.DrawImage(source, destinationRegion, sourceRegion, GraphicsUnit.Pixel);
            }
        }

        /// <summary>
        /// Copies the region into image.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="sourceRegion">The source region.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="destinationRegion">The destination region.</param>
        /// <remarks>Copies regions from one bitmap to another.</remarks>
        private static void CopyRegionIntoImage(Bitmap source, Rectangle sourceRegion, ref Bitmap destination, Rectangle destinationRegion)
        {
            using(Graphics graphics = Graphics.FromImage(destination))
            {                
                graphics.DrawImage(source, destinationRegion, sourceRegion, GraphicsUnit.Pixel);
            }
        }

        /// <summary>
        /// Displays the instructions.
        /// </summary>
        private static void DisplayInstructions()
        {
            MessageBox.Show(String.Format("Please drop a {0} × {1} image on me.",MaxWidth,DisplayHeight));
        }
    }
}