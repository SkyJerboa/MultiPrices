using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MP.Core.Common;
using MP.Core.Common.Constants;
using MP.Core.Common.Heplers;
using MP.Core.Contexts.Games;
using MP.Scraping.Common;
using MP.Scraping.Common.Configuration;
using MP.Scraping.Models.Games;
using MP.Scraping.Models.History;
using MP.Scraping.Models.ServiceGames;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace MP.Scraping.Pages
{
    public class ServiceGameModel : GamePageModel<ServiceGame, SGTranslation, ServiceGameRelationship, SGImage, SGSystemRequirement>
    {
        private ServiceGameContext _context;
        private readonly JsonSerializer _deicmalSerializer = new JsonSerializer
        {
            Culture = CultureInfo.CurrentCulture
        };

        [BindProperty(SupportsGet = true)]
        [FromRoute]
        public string ServiceCode { get; set; }

        public ServiceGame Game { get; set; }
        public List<Change> Changes { get; set; }
        public List<Language> Languages { get; set; }
        public List<PriceInfo> Prices { get; set; }
        public List<Currency> Currencies { get; set; }

        protected override DbContext GameContext => _context;

        public ServiceGameModel(ServiceGameContext db, IConfiguration configuration)
        {
            _configuration = configuration;
            _context = db;
        }

        public IActionResult OnGet()
        {
            if (ServiceCode != null)
            {
                int? id = _context.Games
                    .FirstOrDefault(i => i.MainGameID == Id && i.ServiceCode == ServiceCode.ToUpper())?.ID;

                if (id == null)
                    return new NotFoundResult();
                else
                    return new RedirectResult($"/servicegame/{id}");
            }

            GetGame();

            if (Game == null)
                return NotFound();
            else
                return Page();
        }

        void GetGame()
        {
            Game = _context.Games
                .Include(i => i.Children)
                    .ThenInclude(i => i.Child)
                .Include(i => i.SystemRequirements)
                .Include(i => i.Translations)
                .Include(i => i.Images)
                .FirstOrDefault(i => i.ID == Id);

            if (Game == null)
                return;

            ServiceCode = Game.ServiceCode;

            using (GameContext gameContext = new GameContext(ScrapingConfigurationManager.Config.SiteConnection))
            {
                Languages = gameContext.Languages.ToList();
                Currencies = gameContext.Currencies.ToList();
                Prices = gameContext.PriceInfos
                    .Where(i => i.GameID == Game.MainGameID && i.ServiceCode == ServiceCode)
                    .Include(i => i.Prices)
                    .ToList();
            }

            using (HistoryContext hc = new HistoryContext())
                Changes = hc.Changes.Where(i => i.GameID == Game.MainGameID && i.ChangedFields != null).ToList();
        }

        protected override void SaveGameInfo(JToken jToken)
        {
            ServiceGame game = jToken.ToObject<ServiceGame>();

            Game = _context.Games.FirstOrDefault(i => i.ID == Id);
            if (Game == null)
                return;

            game.ImagesPath = game.ImagesPath.CreateOneLineString();

            if (Game.ImagesPath != game.ImagesPath)
                MoveImagesAndUpdateDB(Game.ImagesPath, game.ImagesPath);

            if (Game.Status != game.Status && game.Status == ServiceGameStatus.Deleted)
                MakeUnavailableAllPriceInfos(Game);

            Game.Name = game.Name;
            Game.InnerID = game.InnerID;
            Game.OfferID = game.OfferID;
            Game.Platforms = game.Platforms;
            Game.AvailabilityDate = game.AvailabilityDate;
            Game.IsAvailable = game.IsAvailable;
            Game.ImagesPath = game.ImagesPath;
            Game.Status = game.Status;
        }

        protected override void SaveJsonData(JObject jo, string type)
        {
            ServiceGame sg = _context.Games.FirstOrDefault(i => i.ID == Id);
            if (sg == null)
                return;

            int mainGameId = sg.MainGameID;
            ServiceCode = sg.ServiceCode;

            if (type == "price-infos")
            {
                List<PriceInfo> piList = jo["PriceInfos"].ToObject<List<PriceInfo>>(_deicmalSerializer);
                List<int> piLeft = piList.Select(i => i.ID).Where(i => i != 0).ToList();

                using (GameWithHistoryContext gameContext = new GameWithHistoryContext())
                {
                    List<PriceInfo> existingPi = gameContext.PriceInfos
                        .Include(i => i.Prices)
                        .Where(i => i.GameID == mainGameId && i.ServiceCode == ServiceCode)
                        .ToList();

                    List<PriceInfo> piToDelete = existingPi.Where(i => !piLeft.Contains(i.ID)).ToList();
                    List<PriceInfo> piToAdd = piList.Where(i => i.ID == 0).ToList();

                    foreach (PriceInfo pi in piToAdd)
                    {
                        pi.ServiceCode = ServiceCode.ToUpper();
                        pi.GameID = mainGameId;
                    }

                    List<Price> pToDelete = new List<Price>();
                    List<Price> pToAdd = new List<Price>();

                    foreach (PriceInfo pi in piList.Where(i => i.ID != 0))
                    {
                        PriceInfo piInDb = existingPi.FirstOrDefault(i => i.ID == pi.ID);
                        piInDb.CompareAndChange(pi);

                        foreach (Price p in pi.Prices)
                        {
                            if (p.ID == 0)
                            {
                                p.ServicePriceID = pi.ID;
                                pToAdd.Add(p);
                            }
                            else
                            {
                                piInDb.Prices.FirstOrDefault(i => i.ID == p.ID)?.CompareAndChange(p);
                            }
                        }

                        List<int> pLeft = pi.Prices.Select(i => i.ID).Where(i => i != 0).ToList();
                        pToDelete.AddRange(piInDb.Prices.Where(i => !pLeft.Contains(i.ID)));
                    }

                    gameContext.PriceInfos.AddRange(piToAdd);
                    gameContext.PriceInfos.RemoveRange(piToDelete);

                    gameContext.Prices.AddRange(pToAdd);
                    gameContext.Prices.RemoveRange(pToDelete);

                    gameContext.SaveChanges(User.Identity.Name);
                }
            }
            else if (type == "service-images")
            {
                int[] imgToDelIds = jo["deleted"]?.ToObject<int[]>() ?? new int[0];
                string imgFolder = _configuration.GetSection("ImageConfiguration")["ImageFolderPath"];
                ServiceGame game = _context.Games.FirstOrDefault(i => i.ID == Id);
                string serviceImgFolder = ServiceCode.ToLower();
                string gameImgFolder = $"{imgFolder}/{serviceImgFolder}/{game.ImagesPath}";

                LoadAndSaveServiceImages(jo["added"] as JArray, game);

                List<SGImage> imgsNeedToDel = _context.Images.Where(i => imgToDelIds.Contains(i.ID)).ToList();
                List<SGImage> imgsToDel = new List<SGImage>();

                using (GameWithHistoryContext gContext = new GameWithHistoryContext())
                {
                    foreach (SGImage img in imgsNeedToDel)
                    {
                        imgsToDel.Add(img);

                        List<GImage> fImgs = gContext.Images.Where(i => i.Name == img.Name).ToList();
                        if (fImgs.Count != 0 && fImgs.Any(i => i.Path == $"{serviceImgFolder}/{img.Path}"))
                            continue;

                        FileHelper.DeleteFilesWithPattern(gameImgFolder, $"{img.Name}*");
                    }

                    FileHelper.DeleteEmptyFolder(gameImgFolder);
                }

                _context.Images.RemoveRange(imgsToDel);
            }
            else if (type == "apply-localization")
            {
                if (sg.Languages == null)
                    return;

                using (GameWithHistoryContext gContext = new GameWithHistoryContext())
                {
                    Game mainGame = gContext.Games.FirstOrDefault(i => i.ID == mainGameId);

                    mainGame.MergeLocalizations(sg.Languages);

                    gContext.SaveChanges(User.Identity.Name);
                }
            }
            else if (type == "apply-image")
            {
                int id = (int)jo["id"];

                SGImage img = _context.Images.FirstOrDefault(i => i.ID == id);
                if (img == null)
                    return;

                using (GameWithHistoryContext gContext = new GameWithHistoryContext())
                {
                    if (img.Tag != ImageTags.SCREENSHOT 
                        && gContext.Images.FirstOrDefault(i => i.GameID == mainGameId && i.Tag == img.Tag) != null)
                        return;

                    if (img.Tag == ImageTags.SCREENSHOT
                        && gContext.Images
                            .FirstOrDefault(i => i.GameID == mainGameId && i.Tag == img.Tag && i.Name == img.Name) != null)
                        return;

                    gContext.Images.Add(new GImage
                    {
                        GameID = mainGameId,
                        Name = img.Name,
                        MediaType = img.MediaType,
                        Order = 1,
                        Path = $"{ServiceCode.ToLower()}/{img.Path}",
                        Tag = img.Tag
                    });

                    gContext.SaveChanges(User.Identity.Name);
                }
            }
            else if (type == "apply-system-requirements")
            {
                List<SGSystemRequirement> sysReqs = _context.SystemRequirements.Where(i => i.GameID == sg.ID).ToList();
                if (sysReqs.Count == 0)
                    return;

                using (GameWithHistoryContext gContext = new GameWithHistoryContext())
                {
                    List<GSystemRequirement> existingSR = gContext.SystemRequirements.Where(i => i.GameID == sg.MainGameID).ToList();
                    foreach(SGSystemRequirement sr in sysReqs)
                    {
                        GSystemRequirement esr = existingSR.FirstOrDefault(i => i.Type == sr.Type && i.SystemType == sr.SystemType);
                        if (esr == null) 
                        {
                            GSystemRequirement newsr = new GSystemRequirement();
                            newsr.GameID = sg.MainGameID;
                            newsr.Type = sr.Type;
                            newsr.SystemType = sr.SystemType;
                            newsr.CompareAndChange(sr);
                            gContext.SystemRequirements.Add(newsr);
                        }
                        else
                        {
                            esr.CompareAndChange(sr);
                        }
                    }

                    gContext.SaveChanges();
                }
            }
        }

        protected override List<ServiceGameRelationship> CreateNewGameRelations(IEnumerable<int> newChildren)
        {
            ServiceCode = _context.Games.FirstOrDefault(i => i.ID == Id).ServiceCode;

            return newChildren
                .Select(i => new ServiceGameRelationship
                {
                    ParentID = Id,
                    ChildID = i,
                    ServiceCode = ServiceCode
                })
                .ToList();
        }

        protected override void SaveFormData()
        {
            return;
        }

        protected override void SaveChanges() => _context.SaveChanges(User.Identity.Name);

        protected override void MoveImagesAndUpdateDB(string oldFolderName, string newFolderName)
        {
            List<string> moveFilesNames = ChangeImgsPathsInDB(oldFolderName, newFolderName);
            ChangeImgsPathInMainGameDB(oldFolderName, newFolderName);

            string imgsServerFolder = _configuration.GetSection("ImageConfiguration")["ImageFolderPath"];
            string oldImgFolderAbsolutePath = Path.Combine(imgsServerFolder, Game.ServiceCode.ToLower(), oldFolderName);
            string newImgFolderAbsolutePath = Path.Combine(imgsServerFolder, Game.ServiceCode.ToLower(), newFolderName);

            MoveImagesToNewDirectory(oldImgFolderAbsolutePath, newImgFolderAbsolutePath, moveFilesNames);
        }

        private void ChangeImgsPathInMainGameDB(string oldFolderName, string newFolderName)
        {
            string oldSGImgsLocalPath = $"{Game.ServiceCode.ToLower()}/{oldFolderName}";
            string newSGImgsLocalPath = $"{Game.ServiceCode.ToLower()}/{newFolderName}";
            int oldLocalPathLength = oldSGImgsLocalPath.Length + 1;

            using (GameWithHistoryContext gContext = new GameWithHistoryContext())
            {
                List<GImage> gImagesInDB = gContext.Images.Where(i => i.GameID == Game.MainGameID).ToList();
                var gImgsFromSGFolder = gImagesInDB.Where(i => i.Path.StartsWith(oldSGImgsLocalPath));
                foreach (GImage gImg in gImgsFromSGFolder)
                    gImg.Path = $"{newSGImgsLocalPath}/{gImg.Path.Substring(oldLocalPathLength)}";

                gContext.SaveChanges(User.Identity.Name);
            }
        }

        private void MakeUnavailableAllPriceInfos(ServiceGame game)
        {
            using (GameWithHistoryContext gContext = new GameWithHistoryContext())
            {
                List<PriceInfo> priceInfos = gContext.PriceInfos
                    .Where(i => i.IsAvailable && i.ServiceCode == game.ServiceCode && i.GameID == game.MainGameID)
                    .ToList();
                priceInfos.ForEach(i => i.IsAvailable = false);

                gContext.SaveChanges(User.Identity.Name);
            }
        }

        async void LoadAndSaveServiceImages(JArray jArray, ServiceGame game)
        {
            if (jArray == null)
                return;

            string imgPath = _configuration.GetSection("ImageConfiguration")["ImageFolderPath"];

            List<SGImage> imgsToAdd = new List<SGImage>();

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "C# App");

            foreach (JToken jt in jArray)
            {
                string tag = jt["tag"].ToString();
                string url = jt["url"].ToString();

                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (!response.Content.Headers.ContentType.MediaType.StartsWith("image"))
                        continue;

                    string fileName = Common.Helpers.StringHelper.CreateGuidString();
                    string extension = Common.Helpers.StringHelper.GetFileExtensionByMimeType(response.Content.Headers.ContentType.ToString());

                    string dirPath = $"{imgPath}/{ServiceCode}/{game.ImagesPath}";
                    if (!Directory.Exists(dirPath))
                        Directory.CreateDirectory(dirPath);

                    string filePath = $"{dirPath}/{fileName}.{extension}";
                    using (var fileStream = System.IO.File.Create(filePath))
                    {
                        Stream str = await response.Content.ReadAsStreamAsync();
                        str.CopyTo(fileStream);
                    }

                    SGImage img = new SGImage
                    {
                        GameID = game.ID,
                        MediaType = Core.GameInterfaces.MediaType.Image,
                        Name = fileName,
                        SourceUrl = url,
                        Tag = tag,
                        Path = $"{game.ImagesPath}/{fileName}.{extension}"
                    };

                    imgsToAdd.Add(img);
                }
                catch(Exception ex)
                {
                    Serilog.Log.Error(ex, "Loading image error");
                }
            }

            client.Dispose();

            using (ServiceGameContext gc = new ServiceGameContext())
            {
                gc.Images.AddRange(imgsToAdd);
                gc.SaveChanges();
            }
        }
    }
}