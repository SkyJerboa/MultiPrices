using Microsoft.CodeAnalysis;
using MP.Scraping.Common.ServiceScripts;
using MP.Scraping.Common.Tasks;
using MP.Scraping.GameProcessing;
using MP.Scraping.Models.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MP.Scraping.Common
{
    public static class ServiceScraper
    {
        private static Dictionary<string, ScraperInfo> _scraperServicesInfo = new Dictionary<string, ScraperInfo>();
        private static List<string> _tasksNames = new List<string>();

        private const int minutesInDay = 60 * 24;
        private const int minMinutesInterval = 10;
        
        private static bool _isConfigured;
        private static bool _isAssemblyReloading;

        private static object _scrapLocker = new object();

        public static void ConfigureScraping()
        {
            if (_isConfigured)
                return;

            try
            {
                ScriptLoader.CompileScripts();
                ScriptLoader.LoadAssembly();
                RunScheduler();
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Scraping Configuring failed");
            }

            _isConfigured = true;
        }

        public static bool SafelyReloadAssembly()
        {
            lock (_scrapLocker)
            {
                if (_isAssemblyReloading || _scraperServicesInfo.Any(i => i.Value.ScrapingStatus != ScrapingStatus.Free))
                    return false;

                _isAssemblyReloading = true;

                foreach (var service in _scraperServicesInfo)
                {
                    ScraperInfo si = service.Value;

                    si.ScrapingStatus = ScrapingStatus.AssemblyUnloaded;

                    si.Task?.Dispose();
                    si.Task = null;
                }
            }


            StopTimer();
            ScriptLoader.UnloadAssembly();
            ScriptLoader.LoadAssembly();
            RunScheduler();
            FreeAllScraps();

            static void StopTimer()
            {
                _tasksNames.ForEach(i => SchedulerService.StopAndRemoveTimer(i));
                _tasksNames.Clear();
            }

            static void FreeAllScraps()
            {
                lock (_scrapLocker)
                {
                    _isAssemblyReloading = false;

                    foreach (var service in _scraperServicesInfo)
                        service.Value.ScrapingStatus = ScrapingStatus.Free;
                }
            }

            return true;
        }

        static void RunScheduler()
        {
            using (ServiceContext context = new ServiceContext())
            {
                foreach (Service service in context.Services)
                {
                    if (!_scraperServicesInfo.ContainsKey(service.Code))
                        _scraperServicesInfo.Add(service.Code, new ScraperInfo(service.Code, service.TypeName));
                    else
                        _scraperServicesInfo[service.Code].TypeName = service.TypeName;

                    if (!service.Enabled)
                    {
                        if (_scraperServicesInfo.ContainsKey(service.Code))
                            _scraperServicesInfo.Remove(service.Code);
                        
                        continue;
                    }
                    
                    DateTime startTime = DateTime.Now.Date.AddMinutes(service.ScrapingStartTime.TotalMinutes);
                    int minutesInterval = service.MinutesInterval ?? minutesInDay;
                    if (minutesInterval < minMinutesInterval)
                        minutesInterval = minMinutesInterval;

                    //временное решение для единственной страны
                    ServiceRequestOptions paramters = new ServiceRequestOptions("RU", "RUB", "ru", true, false);
                    string taskName = $"scraping_{service.Code}";
                    _tasksNames.Add(taskName);
                    
                    SchedulerService.AddTask(taskName, minutesInterval, TimePeriod.Minutes, startTime, () => RunScraping(service.Code, paramters));
                }
            }
        }

        public static bool RunScraping(string serviceCode, ServiceRequestOptions parameters)
        {
            lock (_scrapLocker)
            {
                ScraperInfo info = _scraperServicesInfo[serviceCode.ToUpper()];
                if (info.ScrapingStatus != ScrapingStatus.Free)
                    return false;

                info.ScrapingStatus = ScrapingStatus.Requests;
                info.Task?.Dispose();

                Task task = Task.Run(() =>
                {
                    Type serviceType = ScriptLoader.GetTypeFromScriptAssembly(info.TypeName);
                    if (serviceType == null)
                    {
                        info.ScrapingStatus = ScrapingStatus.Free;
                        Log.Warning($"Type {info.TypeName} undefined");
                        return;
                    }

                    GameService service = (GameService)Activator.CreateInstance(serviceType, args: new object[] { parameters });

                    service.OnStepChanged += (CollectingDataStep step) =>
                    {
                        if (step == CollectingDataStep.DownloadingImages)
                            info.ScrapingStatus = ScrapingStatus.WaitingDownloadingImages;
                    };

                    try
                    {
                        service.CollectData();
                        info.ScrapingStatus = ScrapingStatus.Save;
                        service.SaveCollectedData();
                    }
                    catch (Exception ex)
                    {
                        info.LastException = ex;
                        service.RequestsSumInfo.Exceptions = ex.ToString();
                        service.RequestsSumInfo.EndTime = DateTime.Now;

                        Log.Error(ex, "Collecting data error");
                    }
                    finally
                    {
                        service.Dispose();
                        using (ServiceContext context = new ServiceContext())
                        {
                            context.ServiceRequests.Add(service.RequestsSumInfo);
                            context.SaveChanges();
                        }
                        
                        lock (_scrapLocker)
                            info.ScrapingStatus = ScrapingStatus.Free;
                    }
                });

                info.Task = task;

                return true;
            }
        }

        public static ScrapingStatus GetScrapingStatus(string code) => _scraperServicesInfo[code].ScrapingStatus;
    }


    public enum ScrapingStatus
    {
        Free,
        Requests,
        Save,
        WaitingDownloadingImages,
        AssemblyUnloaded
    }

    class ScraperInfo
    {
        public string ServiceCode;
        public ScrapingStatus ScrapingStatus;
        public Task Task;
        public string TypeName;
        public Exception LastException;

        public ScraperInfo(string code, string typeName)
        {
            ServiceCode = code;
            ScrapingStatus = ScrapingStatus.Free;
            TypeName = typeName;
        }
    }
}
