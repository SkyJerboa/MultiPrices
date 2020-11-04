namespace MP.Scraping.Common.Helpers
{
    public static class WebHelper
    {
        public static string GetImageExtensionByMimeType(string mimeType)
        {
            switch(mimeType)
            {
                case "image/gif": return ".gif";
                case "image/jpg":
                case "image/jpeg": return ".jpg";
                case "image/png": return ".png";
                case "image/svg+xml": return ".svg";
                case "image/tiff": return ".tiff";
                case "image/webp": return ".webp";
                default: return null;
            }
        }
    }
}
