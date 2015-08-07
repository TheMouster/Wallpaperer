using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.IO;

namespace Wallpaperer.Droplet
{
    public class Program
    {
        private static readonly Int32 TotalMonitorWidth;
        private static readonly Int32 MaximumMonitorHeight;
        private static readonly Byte BezelWidth;
        private static readonly Byte DisplayCount;
        private static readonly Byte JPEGQuality;

        /// <summary>
        /// Initializes the <see cref="Program"/> class.
        /// </summary>
        /// <remarks>Reads the settings from the settings configuration file.</remarks>
        static Program()
        {
            BezelWidth = Settings.Default.BezelWidth;
            JPEGQuality = Settings.Default.JPEGQuality;

            DisplayCount = (Byte)Screen.AllScreens.Length;

            //Compute the maximum width (MaxWidth) and maximum height (MaxHeight) that the wallpaper should be.
            TotalMonitorWidth = 0; MaximumMonitorHeight = 0;
            foreach(var display in Screen.AllScreens)
            {
                TotalMonitorWidth += display.Bounds.Width;
                MaximumMonitorHeight = Math.Max(display.Bounds.Height, MaximumMonitorHeight);
            }
            TotalMonitorWidth += BezelWidth * ( ( DisplayCount - 1 ) * 2 );            
        }

        /// <summary>
        /// Takes a screen-size + internal bezel width sized bitmap and crops it to remove the area taken up by the bezels. Making the image appear as if it was seen through a window.
        /// </summary>
        /// <param name="args">The arguments. The path to a valid bitmap file.</param>
        /// <remarks>Writes a file of the same name with -wallpapered appended to the end in PNG format to the same location as the source.</remarks>
        public static void Main(String[] filePaths)
        {
            if(filePaths.Length < 1)
            {
                DisplayInstructions();
                return;
            }

            foreach(var filePath in filePaths)
            {
                String fileName = Path.GetFileNameWithoutExtension(filePath);
                String fileDirectory = Path.GetDirectoryName(filePath);
                String jpgFilePath = String.Format("{0}{1}{2}-wallpapered.jpg", fileDirectory, Path.DirectorySeparatorChar, fileName);

                using(Bitmap sourceBitmap = new Bitmap(filePath))
                {
                    //Check size
                    if(sourceBitmap.Width != TotalMonitorWidth || sourceBitmap.Height != MaximumMonitorHeight)
                    {
                        DisplayInstructions();
                        continue;
                    }

                    Bitmap wallpaper = CreateWallpaper(sourceBitmap);

                    //Save as JPEG
                    var jpegImageCodecInfo = GetEncoderInfo("image/jpeg");
                    var qualityEncoder = Encoder.Quality;
                    var encoderParameters = new EncoderParameters(1);
                    var encoderParameter = new EncoderParameter(qualityEncoder, (Int64)JPEGQuality);
                    encoderParameters.Param[0] = encoderParameter;
                    wallpaper.Save(jpgFilePath, jpegImageCodecInfo, encoderParameters);
                }    
            }
        }

        /// <summary>
        /// Creates a wallpaper bitmap.
        /// </summary>
        /// <param name="sourceBitmap">The source bitmap.</param>
        /// <returns>The bitmap cropped to allow for the monitor bezel.</returns>
        private static Bitmap CreateWallpaper(Bitmap sourceBitmap)
        {
            if(BezelWidth <= 0)
                return sourceBitmap;
            
            //Wallpaper Bitmap
            Int32 croppedWidth = TotalMonitorWidth - ( ( DisplayCount - 1 ) * ( BezelWidth * 2 ) );
            Bitmap wallpaper = new Bitmap(croppedWidth, MaximumMonitorHeight);

            //Set up cropping region and bitmap
            Size area = new Size(Screen.AllScreens[0].Bounds.Width, MaximumMonitorHeight);
            Point sourceOrigin = new Point(0, 0);
            Point destinationOrigin = new Point(0, 0);
            Rectangle sourceRegion = new Rectangle(sourceOrigin, area);
            Rectangle destinationRegion = new Rectangle(sourceOrigin, area);

            Int32 monitorOrigin = 0, displayOrigin = 0;
            for(int i = 0; i < DisplayCount; i++)
            {
                //Compute bitmap source point and area
                sourceOrigin.X = monitorOrigin;
                sourceRegion.Location = sourceOrigin;
                sourceRegion.Width = Screen.AllScreens[i].Bounds.Width;

                //Compute bitmap destination point and area
                destinationOrigin.X = displayOrigin;
                destinationRegion.Location = destinationOrigin;
                destinationRegion.Width = Screen.AllScreens[i].Bounds.Width;

                CopyRegionIntoImage(sourceBitmap, sourceRegion, ref wallpaper, destinationRegion);

                //Move origins to next point
                Int32 currentMonitorWidth = Screen.AllScreens[Math.Max(i, i - 1)].Bounds.Width + ( BezelWidth * 2 );
                Int32 currentDisplayWidth = Screen.AllScreens[Math.Max(i, i - 1)].Bounds.Width;
                //NOTE: versus display width. Youʼre cropping from an image the width of the monitors (including bezels) and pasting into an image the size of the displays.
                monitorOrigin += currentMonitorWidth;
                displayOrigin += currentDisplayWidth;
            }

            return wallpaper;
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
            MessageBox.Show(String.Format("Please drop a {0} × {1} image on me.",TotalMonitorWidth,MaximumMonitorHeight));
        }

        /// <summary>
        /// Gets the MIME encoder information.
        /// </summary>
        /// <param name="mimeType">The MIME type you want an encoder for.</param>
        /// <returns>The ImageCodeInfo struct for the specified MIME type.</returns>
        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            var encoders = ImageCodecInfo.GetImageEncoders();
            for(int i = 0; i < encoders.Length; ++i)
            {
                if(encoders[i].MimeType == mimeType)
                    return encoders[i];
            }
            return null;
        }
    }
}
