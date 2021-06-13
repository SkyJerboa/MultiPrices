using MP.Scraping.Common.Configuration;
using MP.Scraping.Common.Ftp;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MP.Scraping.Common.Tools
{
    public static class ImageSynchronizer
    {
        private static object _ftpActionlocker = new object();
        private static object _toggleLocker = new object();

        public static bool IsInActive { get; private set; }

        
        public static List<string> CleanupFtp(IEnumerable<string> imagesPaths)
        {
            if (!CanCleanupFtp())
                return null;

            lock (_ftpActionlocker)
            {
                List<string> deletedFiles = new List<string>();
                try
                {
                    Task readFilesPathsTask = FtpManager.ForEachFile("/", (filePath) =>
                    {
                        if (!imagesPaths.Contains(filePath))
                        {
                            FtpManager.DeleteFile(filePath);
                            deletedFiles.Add(filePath);
                        }
                    });
                    readFilesPathsTask.Wait();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Cleanup ftp images error");
                }
                finally
                {
                    IsInActive = false;
                }
                
                return deletedFiles;
            }
        }

        public static Task<List<string>> CleanupFtpAsync(IEnumerable<string> imagesPaths) =>
            Task.Run(() => CleanupFtp(imagesPaths));

        public static List<string> UploadMissingImagesToFtp(IEnumerable<string> imagesPaths)
        {
            if (!CanSyncImages())
                return null;

            lock(_ftpActionlocker)
            {
                string imgFolder = ScrapingConfigurationManager.Config.ImageConfiguration.ImageFolderPath;
                List<string> uploadedFiles = new List<string>();
                try
                {
                    foreach(string imgPath in imagesPaths)
                    {
                        string serverFilePath = imgFolder + "/" + imgPath;
                        string ftpFilePath = "/" + imgPath;
                        if (!File.Exists(serverFilePath))
                            continue;

                        bool isFileOnFtp = FtpManager.FileExists(ftpFilePath).Result;
                        if (isFileOnFtp)
                            continue;
                        else
                            FtpManager.UploadFile(serverFilePath, ftpFilePath);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Uploading missing ftp images error");
                }
                finally
                {
                    IsInActive = false;
                }

                return uploadedFiles;
            }
        }

        public static Task<List<string>> UploadMissingImagesToFtpAsync(IEnumerable<string> imagesPaths) =>
            Task.Run(() => UploadMissingImagesToFtp(imagesPaths));

        private static bool CanCleanupFtp() => 
            ScrapingConfigurationManager.Config.FtpConfiguration.UseFtp && TryCaptureThread();

        private static bool CanSyncImages()
        {
            bool isFtpUse = ScrapingConfigurationManager.Config.FtpConfiguration.UseFtp;
            string imgFolder = ScrapingConfigurationManager.Config.ImageConfiguration.ImageFolderPath;

            return isFtpUse && !String.IsNullOrEmpty(imgFolder) && TryCaptureThread();
        }

        private static bool TryCaptureThread()
        {
            lock (_toggleLocker)
            {
                if (IsInActive)
                    return false;

                IsInActive = true;
                return true;
            }
        }
    }
}
