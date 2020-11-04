using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MP.Core.Common;
using MP.Core.Common.Heplers;
using MP.Core.Contexts.Games;
using MP.Core.GameInterfaces;
using MP.Scraping.Common;
using MP.Scraping.Common.Configuration;
using MP.Scraping.Common.Constants;
using MP.Scraping.Common.Ftp;
using MP.Scraping.Models.Games;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;

namespace MP.Scraping.Pages
{
    public class GameModel : GamePageModel<Game, GTranslation, GameRelationship, GImage, GSystemRequirement>
    {
        readonly GameWithHistoryContext _context;
        public Game Game;

        protected override DbContext GameContext => _context;

        public List<Language> Languages { get; set; }
        public Dictionary<int, string> Tags { get; set; }

        public GameModel(GameWithHistoryContext db, IConfiguration configuration)
        {
            _context = db;
            _configuration = configuration;
            Languages = db.Languages.ToList();
            Tags = db.Tags.ToDictionary(k => k.ID, v => v.Name);
        }

        public IActionResult OnGet()
        {
            GetGame();
            
            if (Game == null)
                return NotFound();
            else
                return Page();
        }

        public ActionResult OnPostSetConfirm()
        {
            string role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (!UserRoles.ALLOWED_ROLES_EDITING.Contains(role))
                return Forbid();

            Game = _context.Games
                .Include(i => i.Images)
                .FirstOrDefault(i => i.ID == Id);

            if (Game.Status == GameStatus.Completed)
                return BadRequest();

            if (ScrapingConfigurationManager.Config.FtpConfiguration.UseFtp)
            {
                string ImgsFolderPath = ScrapingConfigurationManager.Config.ImageFolderPath;
                foreach (string imgPath in Game.Images.Select(i => i.Path))
                {
                    string fileFullPath = Path.Combine(ImgsFolderPath, imgPath);
                    FtpManager.UploadFile(fileFullPath, imgPath);
                }
            }

            Game.Status = GameStatus.Completed;
            _context.SaveChanges();

            return new OkResult();
        }

        private void GetGame()
        {
            Game = _context.Games
                .Include(i => i.Children)
                    .ThenInclude(i => i.Child)
                .Include(i => i.Translations)
                    .ThenInclude(i => i.Language)
                .Include(i => i.SystemRequirements)
                .Include(i => i.Images)
                .Include(i => i.Tags)
                    .ThenInclude(i => i.Tag)
                .SingleOrDefault(i => i.ID == Id);
        }

        protected override void SaveGameInfo(JToken jToken)
        {
            Game game = jToken.ToObject<Game>();

            Game = _context.Games.FirstOrDefault(i => i.ID == Id);

            game.ImagesPath = game.ImagesPath.CreateOneLineString();
            game.NameID = game.NameID.CreateOneLineString();

            if (Game.ImagesPath != game.ImagesPath)
                MoveImagesAndUpdateDB(Game.ImagesPath, game.ImagesPath);

            if (Game.NameID != game.NameID)
            {
                bool isNewNameIdExists = _context.Games.Any(i => i.NameID == game.NameID);
                if (isNewNameIdExists)
                    throw new Exception($"NameID \"{game.NameID}\" already in use");
            }

            Game.Name = game.Name;
            Game.NameID = game.NameID;
            Game.Brand = game.Brand;
            Game.Status = game.Status;
            Game.GameType = game.GameType;
            Game.GamePlatform = game.GamePlatform;
            Game.Publisher = game.Publisher;
            Game.Developer = game.Developer;
            Game.ReleaseDate = game.ReleaseDate;
            Game.ImagesPath = game.ImagesPath;
            Game.Order = game.Order;
        }

        protected override void SaveJsonData(JObject jo, string type)
        {
            if (type == "tags")
            {
                string ids = jo.Value<string>("Tag");

                if (String.IsNullOrEmpty(ids))
                {
                    _context.GameTagRelations.RemoveRange(_context.GameTagRelations.Where(i => i.GameID == Id));
                    return;
                }

                List<GameTagRelation> tags = _context.GameTagRelations.Where(i => i.GameID == Id).ToList();

                int[] idsArr = ids.Split(',').Select(i => Int32.Parse(i)).ToArray();
                List<GameTagRelation> tagsToDelete = tags.Where(i => !idsArr.Contains(i.TagID)).ToList();
                List<GameTagRelation> tagsToAdd = idsArr
                    .Where(i => !tags.Any(t => t.TagID == i))
                    .Select(i => new GameTagRelation
                    {
                        GameID = Id,
                        TagID = i
                    })
                    .ToList();

                _context.GameTagRelations.RemoveRange(tagsToDelete);
                _context.GameTagRelations.AddRange(tagsToAdd);
            }
            else if (type == "appy-to-children")
            {
                string parameter = jo["parameter"].ToString();
                Game = _context.Games
                    .Include(i => i.Children)
                        .ThenInclude(i => i.Child)
                    .FirstOrDefault(i => i.ID == Id);

                //может быть стоит убрать это условие
                if (Game.GameType != GameEntityType.FullGame)
                    return;

                switch (parameter)
                {
                    case "Brand":
                        ApplyToChildren(c => c.Brand = Game.Brand);
                        break;
                    case "Developer":
                        ApplyToChildren(c => c.Developer = Game.Developer);
                        break;
                    case "Publisher":
                        ApplyToChildren(c => c.Publisher = Game.Publisher);
                        break;
                    case "Languages":
                        //ApplyToChildren(c => c.MergeLocalizations(Game.Languages));
                        ApplyToChildren(c => c.Languages = Game.Languages);
                        break;
                    case "SystemRequirements":
                        List<GSystemRequirement> gSysReqs = _context.SystemRequirements.Where(i => i.GameID == Game.ID).ToList();
                        ApplyToChildren(c =>
                        {
                            List<GSystemRequirement> sysReqs = _context.SystemRequirements.Where(i => i.GameID == c.ID).ToList();

                            foreach (GSystemRequirement sysReq in gSysReqs)
                            {
                                SystemRequirement existingSysReq = sysReqs
                                    .FirstOrDefault(i => i.GameID == c.ID && sysReq.Type == i.Type && sysReq.SystemType == i.SystemType);

                                if (existingSysReq == null)
                                {
                                    GSystemRequirement newSr = (GSystemRequirement)sysReq.Clone();
                                    newSr.GameID = c.ID;
                                    _context.SystemRequirements.Add(newSr);
                                }
                                else
                                {
                                    //existingSysReq.MergeSystemRequirements(sysReq);
                                    existingSysReq.CompareAndChange(sysReq);
                                }
                            }
                        });
                        break;
                    case "Tags":
                        List<GameTagRelation> gGtRels = _context.GameTagRelations.Where(i => i.GameID == Game.ID).ToList();
                        ApplyToChildren(c => 
                        {
                            List<GameTagRelation> gtRels = _context.GameTagRelations.Where(i => i.GameID == c.ID).ToList();
                            foreach(GameTagRelation rel in gGtRels)
                            {
                                if (!gtRels.Any(i => i.TagID == rel.TagID))
                                    _context.GameTagRelations.Add(new GameTagRelation { GameID = c.ID, TagID = rel.TagID });
                            }
                        });
                        break;
                }

                void ApplyToChildren(Action<Game> act)
                {
                    foreach (Game c in Game.Children.Select(i => i.Child))
                        act(c);
                }
            }
        }

        protected override void SaveFormData()
        {
            string dirPath = _configuration["ImageFolder"];
            string imagesPath = _context.Games.First(i => i.ID == Id).ImagesPath;
            List<GImage> imgsToAdd = new List<GImage>();
            List<GImage> imgsToDelete = new List<GImage>();

            int startOrder = 1;
            string ordering = Request.Form["order"].ToString();
            if (!String.IsNullOrEmpty(ordering))
            {
                int[] orders = ordering.Split(',').Select(i => Int32.Parse(i)).ToArray();
                List<GImage> orderingImgs = _context.Images.Where(i => orders.Contains(i.ID)).ToList();
                foreach(int o in orders)
                {
                    GImage img = orderingImgs.FirstOrDefault(i => i.ID == o);
                    if (img == null)
                        continue;

                    img.Order = startOrder++;
                }
            }

            string folderPath = Path.Combine(dirPath, imagesPath);
            if (Request.Form.Files.Count > 0 && !Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            foreach (var file in Request.Form.Files)
            {
                string name = Common.Helpers.StringHelper.CreateGuidString();
                string extension = Common.Helpers.StringHelper.GetFileExtensionByMimeType(file.ContentType);
                string tag = file.Name;
                string imagePath = Path.Combine(imagesPath, $"{name}.{extension}");
                string savePath = Path.Combine(dirPath, imagePath);

                using (var fileStream = System.IO.File.Create(savePath))
                    file.CopyTo(fileStream);

                imgsToAdd.Add(new GImage
                {
                    GameID = Id,
                    MediaType = MediaType.Image,
                    Name = name,
                    Order = startOrder++,
                    Tag = tag,
                    Path = imagePath.Replace("\\", "/")
                });
            }

            string removeIds = Request.Form["remove"].ToString();
            if (!String.IsNullOrEmpty(removeIds))
            {
                string imgsPath = _context.Games.First(i => i.ID == Id).ImagesPath;
                int[] removeIdsArrInt = removeIds.Split(',').Select(i => Int32.Parse(i)).ToArray();
                imgsToDelete = _context.Images.Where(i => removeIdsArrInt.Contains(i.ID)).ToList();

                foreach (GImage img in imgsToDelete.Where(i => i.Path.StartsWith(Path.Combine(imgsPath, i.Name))))
                {
                    string filePath = Path.Combine(dirPath, img.Path);
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }
            }

            _context.Images.RemoveRange(imgsToDelete);
            _context.Images.AddRange(imgsToAdd);
        }

        protected override void SaveChanges() => _context.SaveChanges(User.Identity.Name);

        protected override void MoveImagesAndUpdateDB(string oldFolderName, string newFolderName)
        {
            List<string> moveFilesNames = ChangeImgsPathsInDB(oldFolderName, newFolderName);

            string imageServerFolder = _configuration["ImageFolder"];
            string oldImgFolderAbsolutePath = Path.Combine(imageServerFolder, oldFolderName);
            string newImgFolderAbsolutePath = Path.Combine(imageServerFolder, newFolderName);

            MoveImagesToNewDirectory(oldImgFolderAbsolutePath, newImgFolderAbsolutePath, moveFilesNames);
        }
    }
}