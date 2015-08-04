using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace Wallpaperer.Droplet
{
    public class Program
    {
        private static readonly Int32 MaxWidth;
        private static readonly Int32 MaxHeight;
        private static readonly Byte BezelWidth;
        private static readonly Byte DisplayCount;

        /// <summary>
        /// Initializes the <see cref="Program"/> class.
        /// </summary>
        /// <remarks>Reads the settings from the settings configuration file.</remarks>
        static Program()
        {
            BezelWidth = Settings.Default.BezelWidth;

            DisplayCount = (Byte)Screen.AllScreens.Length;

            //Compute the maximum width (MaxWidth) and maximum height (MaxHeight) that the wallpaper should be.
            MaxWidth = 0; MaxHeight = 0;
            foreach(var screen in Screen.AllScreens)
            {
                MaxWidth += screen.Bounds.Width;
                MaxHeight = Math.Max(screen.Bounds.Height, MaxHeight);
            }
            MaxWidth += BezelWidth * ( ( DisplayCount - 1 ) * 2 );            
        }

        /// <summary>
        /// Takes a screen-size + internal bezel width sized bitmap and crops it to remove the area taken up by the bezels. Making the image appear as if it was seen through a window.
        /// </summary>
        /// <param name="args">The arguments. The path to a valid bitmap file.</param>
        /// <remarks>Writes a file of the same name with -wallpapered appended to the end in PNG format to the same location as the source.</remarks>
        public static void Main(String[] args)
        {
            if(args.Length != 1)
            {
                DisplayInstructions();
                return;
            }

            try
            {
                String filePath = args[0];
                String fileName = Path.GetFileNameWithoutExtension(filePath);
                String fileDirectory = Path.GetDirectoryName(filePath);
                String newFilePath = String.Format("{0}{1}{2}-wallpapered.png",fileDirectory,Path.DirectorySeparatorChar,fileName);                

                using (Bitmap sourceBitmap = new Bitmap(filePath))
                {
                    //Check size
                    if(sourceBitmap.Width != MaxWidth || sourceBitmap.Height != MaxHeight)
                    {
                        DisplayInstructions();
                        return;
                    }

                    //Wallpaper Bitmap
                    Int32 croppedWidth = MaxWidth - ((DisplayCount - 1) * (BezelWidth * 2));
                    Bitmap wallpaper = new Bitmap(croppedWidth,MaxHeight);

                    //Set up cropping region and bitmap
                    Size area = new Size(Screen.AllScreens[0].Bounds.Width, MaxHeight);
                    Point sourceOrigin = new Point(0, 0);
                    Point destinationOrigin = new Point(0, 0);
                    Rectangle sourceRegion = new Rectangle(sourceOrigin, area);
                    Rectangle destinationRegion = new Rectangle(sourceOrigin,area);                    

                    Int32 monitorOrigin = 0, displayOrigin = 0;                    
                    for (int i = 0; i < DisplayCount; i++)
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
            MessageBox.Show(String.Format("Please drop a {0} × {1} image on me.",MaxWidth,MaxHeight));
        }
    }
}
