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
        static FtpClient _client;
        static FtpConfiguration _config;
        static LinkedTask _ftpTask;
        static CancellationToken _cancelToken;
        static readonly object _sessionLocker = new object();

        static bool _isSessionOpen;

        static FtpManager()
        {
            _config = ScrapingConfigurationManager.Config.FtpConfiguration;
        }

        public static void UploadFile(string sourcePath, string remotePath)
        {
            DoAction(UploadFileAction);
            
            void UploadFileAction()
            {
                FtpStatus status = _client.UploadFile(sourcePath, remotePath, createRemoteDir: true);
                if (status == FtpStatus.Failed)
                    throw new FtpException($"Failed uploading file \"{sourcePath}\"");
            }
        }

        public static void UploadDirectory(string sourcePath, string remotePath)
            => DoAction(() => _client.UploadDirectory(sourcePath, remotePath));

        static void DoAction(Action action)
        {
            lock (_sessionLocker)
            {
                if (!_isSessionOpen)
                    OpenSession();


                LinkedTask currTask = _ftpTask;
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
                        TryCloseSession(currTask);
                    }
                }, _cancelToken);
            }
        }

        #region session
        static void OpenSession()
        {
            if (_client != null)
                return;

            if (!_config.UseFtp)
                throw new Exception("To use FTP, it must be turned on in the appsettings");

            _client = new FtpClient(_config.Server);
            _client.Credentials = new NetworkCredential(_config.UserName, _config.Password);
            _client.Connect();


            _ftpTask = new LinkedTask(LinkedTask.EmptyAction);
            _ftpTask.Task.Start();
            _cancelToken = new CancellationToken();

            _isSessionOpen = true;
        }

        static void CloseSession()
        {
            if (_client == null)
                return;

            _client.Disconnect();
            _client.Dispose();
            _client = null;

            LinkedTask currTask = _ftpTask;
            Task.Run(() =>
            {
                currTask.WaitAllTasks();
                currTask.Dispose();
            });
            _ftpTask = null;

            _isSessionOpen = false;
        }

        static void TryCloseSession(LinkedTask prevTask)
        {
            lock (_sessionLocker)
            {
                if (prevTask.Next.HasNextTask)
                    return;

                CloseSession();
            }
        }
        #endregion
    }
}
