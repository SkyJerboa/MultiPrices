using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MP.Scraping.Common.Configuration;
using MP.Scraping.Common.Constants;
using MP.Scraping.Common.Tools;
using MP.Scraping.Models.Games;

namespace MP.Scraping.Pages
{
    public class ToolsModel : PageModel
    {
        private const string FTP_NOT_USE = "FTP не используется";
        public static string FtpCleanupInfo { get; private set; } = ScrapingConfigurationManager.Config.FtpConfiguration.UseFtp
            ? "Последний запуск не обнаружен"
            : FTP_NOT_USE;
        public static string FtpMissingImgsInfo { get; private set; } = FtpCleanupInfo;

        private GameWithHistoryContext _context;

        public ToolsModel(GameWithHistoryContext gameContext)
        {
            _context = gameContext;
        }
        
        public IActionResult OnPost()
        {
            string role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (!UserRoles.ALLOWED_ROLES_SCRAPING.Contains(role))
                return Forbid();

            if (Request.Form.ContainsKey("action"))
            {
                string action = Request.Form["action"].ToString();
                switch(action)
                {
                    case "ftp-images-cleanup":
                        CleanupImages();
                        break;
                    case "ftp-images-upload-missings":
                        UploadMissingImages();
                        break;
                }
            }
            
            return RedirectToPage("./tools");
        }

        private async void CleanupImages()
        {
            if (!ScrapingConfigurationManager.Config.FtpConfiguration.UseFtp)
                FtpCleanupInfo = FTP_NOT_USE;

            List<string> imagesPaths = _context.Images.Select(i => '/' + i.Path).ToList();
            List<string> deletedFiles = await ImageSynchronizer.CleanupFtpAsync(imagesPaths);
            FtpCleanupInfo = CreateFtpResultString(
                files: deletedFiles,
                fail: "Файлы не были удалены",
                action: "Удалено изображений: ",
                filesDescription: "Удаленные изображения: ");
        }

        private async void UploadMissingImages()
        {
            if (!ScrapingConfigurationManager.Config.FtpConfiguration.UseFtp)
                FtpMissingImgsInfo = FTP_NOT_USE;

            List<string> imagesPaths = _context.Images.Select(i => i.Path).ToList();
            List<string> uploadedImages = await ImageSynchronizer.UploadMissingImagesToFtpAsync(imagesPaths);

            FtpMissingImgsInfo = CreateFtpResultString(
                files: uploadedImages, 
                fail: "Файлы не были загружены",
                action: "Загружено изображений: ", 
                filesDescription: "Удаленные изображения: ");
        }

        private string CreateFtpResultString(List<string> files, string fail, string action, 
            string filesDescription)
        {
            if (files == null)
            {
                return fail;
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(action);
                sb.AppendLine(files.Count.ToString());
                sb.AppendLine(new String('-', 20));
                sb.AppendLine(filesDescription);
                foreach (string filePath in files)
                    sb.AppendLine(filePath);

                return sb.ToString();
            }
        }
    }
}
