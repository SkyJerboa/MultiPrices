using MP.Scraping.Common;
using MP.Scraping.Common.ServiceScripts;
using MP.Scraping.GameProcessing.ScrapedGameModels;
using MP.Scraping.Models.ServiceGames;
using MP.Scraping.Models.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MP.Scraping.GameProcessing
{
    public abstract class GameService : IDisposable
    {
        protected HttpClientHandler ClientHandler = new HttpClientHandler();
        protected HttpClient Client;
        protected Service ServiceInfo { get; }
        protected HashSet<string> GamesInnerIdsInTable { get; }
        protected ScrapedGameCollection CollectedGames { get; } = new ScrapedGameCollection();
        protected ServiceRequestOptions RequestOptions { get; }
        protected CollectingDataStep Step { get; private set; } = CollectingDataStep.NoActions;

        public abstract ServiceConstants ServiceConstantsValues { get; }

        public string ServiceCode { get; }

        public ServiceRequest RequestsSumInfo { get; }

        public delegate void StepChangeHandler(CollectingDataStep step);
        public event StepChangeHandler OnStepChanged;

        object _stepChangerLocker = new object();


        private const string MAP_JSON_FILENAME = "UrlLinks";
        
        private Dictionary<string, string> _requestsUrlFileMap = new Dictionary<string, string>();
        private Task _saveRequestUrlMapTask = new Task(() => { });
        private CancellationTokenSource _cancellationToken = new CancellationTokenSource();
        private readonly string _requestsFilesDir;
        private bool _finishedCorrectly;

        /// <summary>
        /// Создает новый экземпляр игрового сервиса. 
        /// Позволяет скачивать и сохранять информацию в БД.
        /// </summary>
        /// <param name="options">Параметры запроса</param>
        public GameService(string serviceCode, ServiceRequestOptions options)
        {
            Client = new HttpClient(ClientHandler);
            
            ServiceCode = serviceCode;
            RequestOptions = options;

            //загрузка основной информации из БД
            if (RequestOptions.IsAutonomusRun)
            {
                GamesInnerIdsInTable = new HashSet<string>();
                ServiceInfo = new Service
                {
                    Code = serviceCode.ToUpper(),
                    Name = serviceCode
                };
            }
            else
            {
                using (ServiceGameContext gameContext = new ServiceGameContext())
                {
                    GamesInnerIdsInTable = gameContext.Games
                        .Where(i => i.ServiceCode == ServiceCode)
                        .Select(i => i.InnerID).ToHashSet();
                }

                using (ServiceContext sc = new ServiceContext())
                {
                    ServiceInfo = sc.Services
                        .Where(i => i.Code == serviceCode)
                        .FirstOrDefault();
                }
            }

            if (ServiceInfo == null)
                throw new NullReferenceException($"Service with code {serviceCode} doesn't contains in Service table");

            //инициализация статистики текущего сбора и нового списка для хранения соираемой информации
            RequestsSumInfo = new ServiceRequest(ServiceInfo.Code, options, RequestOptions.UserName);
            
            _requestsFilesDir = Path.Combine("Temp", "Responses", ServiceInfo.Name);
            InitializeTempFiles();
        }

        private void InitializeTempFiles()
        {
            bool isDirExists = Directory.Exists(_requestsFilesDir);

            _saveRequestUrlMapTask.Start();

            if (!isDirExists)
                Directory.CreateDirectory(_requestsFilesDir);

            if (File.Exists(Path.Combine(_requestsFilesDir, MAP_JSON_FILENAME)))
            {
                string jsonDoc = File.ReadAllText(Path.Combine(_requestsFilesDir, MAP_JSON_FILENAME));
                _requestsUrlFileMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonDoc);
            }
        }

        public void CollectData()
        {
            lock(_stepChangerLocker)
            {
                if (Step != CollectingDataStep.NoActions)
                    return;

                Step = CollectingDataStep.DownloadingData;
            }
            InvokeChangedStepEvent();

            RequestsSumInfo.StartTime = DateTime.Now;

            RunCollecting();

            lock (_stepChangerLocker)
                Step = CollectingDataStep.DataDownloaded;
            InvokeChangedStepEvent();
        }

        protected abstract void RunCollecting();

        public void SaveCollectedData()
        {
            lock(_stepChangerLocker)
            {
                if (Step != CollectingDataStep.DataDownloaded)
                    return;

                Step = CollectingDataStep.SavingData;
            }
            InvokeChangedStepEvent();

            if (RequestOptions.IsAutonomusRun)
                throw new Exception("Cannot save loaded data in autonomus mode");

            using (CollectedDataSaver dataSaver = new CollectedDataSaver(
                serviceCode: ServiceCode,
                collectedGames: CollectedGames,
                requestsSumInfo: RequestsSumInfo,
                options: RequestOptions,
                token: _cancellationToken.Token))
            {

                dataSaver.GamesSaved += ChangeStepToDownloadingImgs;

                dataSaver.SaveData();


                RequestsSumInfo.EndTime = DateTime.Now;
                _finishedCorrectly = true;
            }

            lock (_stepChangerLocker)
                Step = CollectingDataStep.NoActions;
            InvokeChangedStepEvent();
        }

        void ChangeStepToDownloadingImgs()
        {
            lock (_stepChangerLocker)
            {
                Step = CollectingDataStep.DownloadingImages;
            }
        }

        protected string GetResponse(string url) => GetResponseByUrl(url);

        protected string GetResponsePost(string url, HttpContent content) => GetResponseByUrl(url, content);
    
        private string GetResponseByUrl(string url, HttpContent content = null)
        {
            if (_cancellationToken.Token.IsCancellationRequested)
                CancelScraping();

            string key = (content != null)
                ? url + "_" + content.ReadAsStringAsync().Result
                : url;


            if (_requestsUrlFileMap.ContainsKey(key))
            {
                string filePath = Path.Combine(_requestsFilesDir, _requestsUrlFileMap[key]);
                RequestsSumInfo.RequestsCount++;
                return File.ReadAllText(filePath);
            }
            else
            {
                string res_str = null;

                HttpResponseMessage response = (content == null)
                    ? Client.GetAsync(url).Result
                    : Client.PostAsync(url, content).Result;

                if (!response.IsSuccessStatusCode)
                {
                    Serilog.Log.Warning($"Request to {url} returned {response.StatusCode} status code");
                }
                else
                {
                    res_str = response.Content.ReadAsStringAsync().Result;
                }

                _saveRequestUrlMapTask = _saveRequestUrlMapTask.ContinueWith(t =>
                {
                    if (_requestsUrlFileMap.ContainsKey(key))
                        return;

                    string fileName = "get_response_" + (_requestsUrlFileMap.Count + 1);
                    string filePath = Path.Combine(_requestsFilesDir, fileName);
                    File.WriteAllText(filePath, res_str);
                    _requestsUrlFileMap.Add(key, fileName);
                    SaveRequestMap();
                });

                RequestsSumInfo.RequestsCount++;

                response.Dispose();
                
                return res_str;
            }
        }

        protected void SaveRequestMap()
        {
            string filePath = Path.Combine(_requestsFilesDir, MAP_JSON_FILENAME);
            string text = JsonConvert.SerializeObject(_requestsUrlFileMap, Formatting.Indented);
            File.WriteAllText(filePath, text);
        }

        private void CancelScraping()
        {
            Dispose();
            throw new OperationCanceledException();
        }

        protected bool IsNewGame(string id) => !GamesInnerIdsInTable.Contains(id);

        private void InvokeChangedStepEvent() => OnStepChanged?.Invoke(Step);

        public string SerializeCollectedGames() 
            => JsonConvert.SerializeObject(CollectedGames, Formatting.Indented, new Newtonsoft.Json.Converters.StringEnumConverter());

        public void StopProcess()
        {
            _cancellationToken.Cancel();
        }

        public virtual void Dispose()
        {
            _saveRequestUrlMapTask.Wait();

            if (_finishedCorrectly && !RequestOptions.IsTesting)
                Directory.Delete(_requestsFilesDir, true);

            Client.Dispose();
            _cancellationToken.Dispose();
            _saveRequestUrlMapTask.Dispose();
        }
    }

    public enum CollectingDataStep
    {
        NoActions,
        DownloadingData,
        DataDownloaded,
        SavingData,
        DownloadingImages
    }
}
