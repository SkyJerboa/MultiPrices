using MP.Core.Common.Constants;
using MP.Core.Contexts.Games;
using MP.Core.GameInterfaces;
using MP.Scraping.Common.Helpers;
using MP.Scraping.Common.Tasks;
using MP.Scraping.GameProcessing.ScrapedGameModels;
using MP.Scraping.Models.ServiceGames;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MP.Scraping.GameProcessing
{
    public class ImageDownloader : IDisposable
    {
        private readonly string _imageMapFilePath;
        private readonly string _serviceCode;
        private readonly string _imgServerDirectory;

        private readonly GameBulkSaver _gameSaver;

        private readonly CancellationToken _outerCancellationToken;
        private readonly CancellationTokenSource _downloadCancelToken = new CancellationTokenSource();

        private readonly HttpClient _client = new HttpClient();

        private readonly Dictionary<string, List<SGImage>> _gamesNamesToDownloadedImgsMap 
            = new Dictionary<string, List<SGImage>>();

        private readonly object _imgMapSaveLocker = new object();

        private TaskQueue _imgDownloadLastTask = new TaskQueue(() => { });
        private bool _canDeleteImgsMapFile;
        
        public ImageDownloader(string serviceCode, string serviceImgDirectory, GameBulkSaver gameSaver, CancellationToken token)
        {
            _serviceCode = serviceCode.ToLower();
            _imgServerDirectory = serviceImgDirectory;
            _gameSaver = gameSaver;
            _outerCancellationToken = token;

            _imageMapFilePath = Path.Combine("Temp", "ImageMap", serviceCode.ToLower() + ".json");

            if (!File.Exists(_imageMapFilePath))
            {
                lock (_imgMapSaveLocker)
                    File.WriteAllText(_imageMapFilePath, null);
            }
            else
            {
                string jsonDoc = "";
                lock (_imgMapSaveLocker)
                    jsonDoc = File.ReadAllText(_imageMapFilePath);

                _gamesNamesToDownloadedImgsMap = JsonConvert.DeserializeObject<Dictionary<string, List<SGImage>>>(jsonDoc)
                    ?? new Dictionary<string, List<SGImage>>();
            }

            _imgDownloadLastTask.Task.Start();
        }

        public void AddNewDownloadingImgTask(ScrapedGameImages gameImages, ServiceGame sGame, Game mGame)
        {
            if (_outerCancellationToken.IsCancellationRequested || _downloadCancelToken.IsCancellationRequested)
                return;

            _imgDownloadLastTask = 
                _imgDownloadLastTask.Then(() => DownloadImages(gameImages, sGame, mGame), _outerCancellationToken);
        }

        public void WaitLastTask() => _imgDownloadLastTask.Task.Wait();

        public void DeleteTempFile() => _canDeleteImgsMapFile = true;

        private void DownloadImages(ScrapedGameImages gameImages, ServiceGame sGame, Game mGame)
        {
            if (_outerCancellationToken.IsCancellationRequested)
                CancelDownloading();

            if (gameImages.Horizontal.IsNullOrEmpty() && gameImages.Vertical.IsNullOrEmpty())
                return;

            string imgsDirPath = Path.Combine(_imgServerDirectory, _serviceCode, sGame.ImagesPath);

            if (!Directory.Exists(imgsDirPath))
            {
                Directory.CreateDirectory(imgsDirPath);
            }
            else if (_gamesNamesToDownloadedImgsMap.ContainsKey(sGame.ImagesPath))
            {
                AddDownloadedImagesToGames(sGame.ImagesPath, mGame, sGame);
                return;
            }

            List<SGImage> sgImgs = new List<SGImage>();
            List<GImage> gImgs = new List<GImage>();

            DownloadImageAndAddToLists(gameImages.Vertical, ImageTags.IMG_VERTICAL);
            DownloadImageAndAddToLists(gameImages.Horizontal, ImageTags.IMG_HORIZONTAL);
            DownloadImageAndAddToLists(gameImages.LogoPng, ImageTags.IMG_LOGO);
            DownloadImageAndAddToLists(gameImages.LongHeader, ImageTags.HEADER);

            string[] screenshotsUrls = gameImages.Screenshots ?? new string[0];
            foreach (string sUrl in screenshotsUrls)
                DownloadImageAndAddToLists(sUrl, ImageTags.SCREENSHOT);

            Task.Run(() => AddAndSaveImagesToMap(sGame.ImagesPath, sgImgs));

            _gameSaver.AddImages(sgImgs, gImgs);


            #region local_methods
            void DownloadImageAndAddToLists(string url, string tag, string name = null)
            {
                string fileNameWithExtension = DownloadAndSaveImage(url, sGame.ImagesPath, name);

                if (fileNameWithExtension == null)
                    return;

                AddImgToLists(tag, url, sGame.ImagesPath, fileNameWithExtension);
            }

            void AddImgToLists(string tag, string url, string imgFolderName, string fileNameWithExtension)
            {
                string fileName = fileNameWithExtension.Split('.')[0];

                sgImgs.Add(new SGImage
                {
                    Game = sGame,
                    MediaType = MediaType.Image,
                    Name = fileName,
                    Path = Path.Combine(imgFolderName, fileNameWithExtension).Replace("\\", "/"),
                    Tag = tag,
                    SourceUrl = url
                });

                if (mGame.Status == GameStatus.Completed)
                    return;

                gImgs.Add(new GImage
                {
                    Game = mGame,
                    MediaType = MediaType.Image,
                    Name = fileName,
                    Path = Path.Combine(_serviceCode, imgFolderName, fileNameWithExtension).Replace("\\", "/"),
                    Tag = tag,
                    Order = 1
                });
            }

            
            #endregion
        }

        /// <summary>
        /// Скачивает изображение и сохраняет его на диск. Возвращает имя сохраненного изображения
        /// </summary>
        /// <param name="url">URL изображения</param>
        /// <param name="imgsFolderName">Папка с изображениями игры</param>
        /// <param name="name">Имя изображения, с которым его нужно сохранить. Если не указать параметр, 
        /// то имя сгенерируется автоматически</param>
        /// <returns>Имя скаченного изображения с расширением</returns>
        private string DownloadAndSaveImage(string url, string imgsFolderName, string name = null)
        {
            if (String.IsNullOrEmpty(url))
                return null;

            string fileNameWithExtension = null;

            try
            {
                HttpResponseMessage response = _client.GetAsync(url).Result;
                Stream imgStream = response.Content.ReadAsStreamAsync().Result;

                string fileName = name ?? StringHelper.CreateGuidString();
                string fileAbsolutePath = Path.Combine(_imgServerDirectory, _serviceCode, imgsFolderName, fileName);

                string extension = WebHelper.GetImageExtensionByMimeType(response.Content.Headers.ContentType.MediaType);
                fileName += extension;
                fileAbsolutePath += extension;

                using (var fs = new FileStream(fileAbsolutePath, FileMode.Create, FileAccess.Write))
                    imgStream.CopyTo(fs);

                fileNameWithExtension = fileName;

                imgStream.Dispose();
                response.Dispose();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Downloading image error");
            }

            return fileNameWithExtension;
        }

        private void AddDownloadedImagesToGames(string pathName, Game mainGame, ServiceGame serviceGame)
        {
            List<SGImage> imgs = _gamesNamesToDownloadedImgsMap[pathName];

            List<SGImage> sgImgs = new List<SGImage>();
            List<GImage> gImgs = new List<GImage>();

            foreach (SGImage img in imgs)
            {
                sgImgs.Add(new SGImage
                {
                    Game = serviceGame,
                    MediaType = img.MediaType,
                    Name = img.Name,
                    Path = img.Path.Replace("\\", "/"),
                    Tag = img.Tag,
                    SourceUrl = img.SourceUrl
                });

                if (mainGame.Status == GameStatus.Completed)
                    continue;
                    
                
                gImgs.Add(new GImage
                {
                    Game = mainGame,
                    MediaType = img.MediaType,
                    Name = img.Name,
                    Path = Path.Combine(_serviceCode.ToLower(), img.Path).Replace("\\", "/"),
                    Tag = img.Tag,
                    Order = 1
                });
            }

            _gameSaver.AddImages(sgImgs, gImgs);
        }

        private void AddAndSaveImagesToMap(string folderPath, ICollection<SGImage> imgs)
        {
            List<SGImage> newImgList = new List<SGImage>();

            foreach (SGImage img in imgs)
            {
                newImgList.Add(new SGImage
                {
                    MediaType = img.MediaType,
                    Name = img.Name,
                    Path = img.Path,
                    SourceUrl = img.SourceUrl,
                    Tag = img.Tag
                });
            }

            lock (_imgMapSaveLocker)
            {
                _gamesNamesToDownloadedImgsMap.Add(folderPath, newImgList);
                string text = JsonConvert.SerializeObject(_gamesNamesToDownloadedImgsMap, Formatting.Indented);
                File.WriteAllText(_imageMapFilePath, text);
            }
        }

        private void CancelDownloading()
        {
            StopTasksAndDispose();
            throw new OperationCanceledException();
        }

        public void StopTasksAndDispose()
        {
            _downloadCancelToken.Cancel();
            Dispose();
        }

        public void Dispose()
        {
            _imgDownloadLastTask.WaitAllTasks();

            _client.Dispose();

            if (_canDeleteImgsMapFile)
                lock(_imgMapSaveLocker)
                    File.Delete(_imageMapFilePath);
        }
    }
}
