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
            //return new Bitmap(image, new Size(width, height));

            var destRect = new Rectangle(0, 0, width, height);
            var pixelFormat = GetPixelFormat(image);
            var destImage = new Bitmap(width, height, pixelFormat);

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

        public static Bitmap ReduceImageByHorizontal(Image image, int width)
        {
            if (image.Width <= width)
                return (Bitmap)image.Clone();

            int height = width * image.Height / image.Width;
            return ResizeImage(image, width, height);
        }

        public static Bitmap ReduceImageByVertical(Image image, int height)
        {
            if (image.Height <= height)
                return (Bitmap)image.Clone();

            int width = height * image.Width / image.Height;
            return ResizeImage(image, width, height);
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

        private static PixelFormat GetPixelFormat(Image img)
        {
            PixelFormat pf = img.PixelFormat;
            switch (pf)
            {
                case PixelFormat.Indexed:
                case PixelFormat.Format1bppIndexed:
                case PixelFormat.Format4bppIndexed:
                case PixelFormat.Format8bppIndexed:
                    return PixelFormat.Format16bppRgb555;
                case (PixelFormat)8207:
                    return PixelFormat.Format32bppRgb;
                default:
                    return pf;
            }
        }
    }
}
