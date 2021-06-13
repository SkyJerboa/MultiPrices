using FluentFTP;
using MP.Scraping.Common.Configuration;
using MP.Scraping.Common.Tasks;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MP.Scraping.Common.Ftp
{
    public static class FtpManager
    {
        const int END_SESSION_DELAY = 60 * 1000;

        static FtpClient _client;
        static FtpConfiguration _config;
        static TaskQueue _ftpTask;
        static CancellationToken _cancelToken;
        static Timer _endSessionTimer;
        static readonly object _sessionLocker = new object();

        static bool _isSessionOpen;

        static FtpManager()
        {
            _config = ScrapingConfigurationManager.Config.FtpConfiguration;
        }

        public static void UploadFile(string sourcePath, string remotePath)
        {
            DoActionSafe(UploadFileAction);
            
            void UploadFileAction()
            {
                FtpStatus status = _client.UploadFile(sourcePath, remotePath, FtpRemoteExists.Skip, createRemoteDir: true);
                if (status == FtpStatus.Failed)
                    throw new FtpException($"Failed uploading file \"{sourcePath}\"");
            }
        }

        public async static Task ForEachFile(string rootPath, Action<string> onFileRead)
        {
            await Task.Run(() =>
            {
                ReadAllFilesPaths(rootPath);

                void ReadAllFilesPaths(string path)
                {
                    FtpListItem[] ftpListItems = GetListng(path).Result;

                    foreach (var f in ftpListItems)
                    {
                        if (f.Type == FtpFileSystemObjectType.File)
                            onFileRead(f.FullName);
                        else if (f.Type == FtpFileSystemObjectType.Directory)
                            ReadAllFilesPaths(f.FullName);
                    }
                }
            });
        }

        public static void UploadDirectory(string sourcePath, string remotePath)
            => DoActionSafe(() => _client.UploadDirectory(sourcePath, remotePath));

        public static void DeleteFile(string filePath) =>
            DoActionSafe(() => _client.DeleteFile(filePath));

        public static async Task<bool> FileExists(string filePath) =>
            await GetResult(() => _client.FileExists(filePath));

        private async static Task<FtpListItem[]> GetListng(string path) =>
            await GetResult(() => _client.GetListing(path));

        private async static Task<T> GetResult<T>(Func<T> func)
        {
            T res = default;
            await DoActionSafe(() =>
            {
                res = func();
            }).Task;

            return res;
        }

        private static TaskQueue DoActionSafe(Action action)
        {
            lock (_sessionLocker)
            {
                if (!_isSessionOpen)
                    OpenSession();


                TaskQueue currTask = _ftpTask;
                _ftpTask = _ftpTask.Then(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error(ex.ToString());
                    }
                    finally
                    {
                        TryCloseSessionSafe(currTask);
                    }
                }, _cancelToken);

                return _ftpTask;
            }
        }

        #region session
        private static void OpenSession()
        {
            if (_client != null)
                return;


            if (!_config.UseFtp)
                throw new Exception("To use FTP, it must be turned on in the appsettings");

            _client = new FtpClient(_config.Server);
            _client.Credentials = new NetworkCredential(_config.UserName, _config.Password);
            try
            {
                _client.Connect();
            }
            catch (Exception ex)
            {
                _client.Dispose();
                _client = null;
                throw ex;
            }


            _ftpTask = new TaskQueue(TaskQueue.EmptyAction);
            _ftpTask.Task.Start();
            _cancelToken = new CancellationToken();

            _isSessionOpen = true;
        }

        private static void TryCloseSessionSafe(TaskQueue prevTask)
        {
            lock (_sessionLocker)
            {
                if (prevTask.Next.HasNextTask)
                    return;

                CloseSessionAfterDelay();
            }
        }

        private static void CloseSessionAfterDelay()
        {
            if (_endSessionTimer != null)
                _endSessionTimer.Change(END_SESSION_DELAY, Timeout.Infinite);
            else
                _endSessionTimer = new Timer(_ => CloseSessionSafe(), null, END_SESSION_DELAY, Timeout.Infinite);
        }

        private static void CloseSessionSafe()
        {
            lock (_sessionLocker)
            {
                if (_client == null)
                    return;

                _client.Disconnect();
                _client.Dispose();
                _client = null;

                TaskQueue currTask = _ftpTask;
                Task.Run(() =>
                {
                    currTask.WaitAllTasks();
                    currTask.Dispose();
                });
                _ftpTask = null;

                _endSessionTimer = null;
                _isSessionOpen = false;
            }
        }
        #endregion
    }
}
