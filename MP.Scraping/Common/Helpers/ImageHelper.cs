using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace MP.Scraping.Common.Helpers
{
    public static class ImageHelper
    {
        public const int MAX_HEIGHT = 700;
        public const int MAX_WIDTH = 500;

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
            return destImage;
        }

        public static Bitmap ReduceImage(Image image)
        {
            bool isHorizontal = image.Width > image.Height;

            if ((isHorizontal && image.Height < MAX_HEIGHT) || (!isHorizontal && image.Width < MAX_WIDTH))
                return (Bitmap)image;

            int newHeight = (isHorizontal) ? MAX_HEIGHT : MAX_WIDTH * image.Height / image.Width;
            int newWidth = (isHorizontal) ? image.Width * newHeight / image.Height : MAX_WIDTH;

            return ResizeImage(image, newWidth, newHeight);
        }
    }
}
