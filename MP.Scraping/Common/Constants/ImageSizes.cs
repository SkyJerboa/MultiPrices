using SizeMap = System.Collections.Generic.Dictionary<string, int>;

namespace MP.Scraping.Common.Constants
{
    public static class ImageSizes
    {
        public static readonly string[] SizeSuffixes = new string[] { "lg", "md", "sm" };

        public static readonly SizeMap CoverHorizontal = new SizeMap
        {
            { "lg", 590 },
            { "md", 300 },
            { "sm", 155 }
        };

        public static readonly SizeMap CoverVertical = new SizeMap
        {
            { "lg", 310 },
            { "md", 154 },
            { "sm", 87 }
        };

        public static readonly SizeMap ScreenshotHorizontal = new SizeMap
        {
            { "md", 284 },
            { "sm", 177 }
        };
    }
}
