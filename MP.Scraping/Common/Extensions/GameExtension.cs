using MP.Core.Common.Constants;
using MP.Core.Contexts.Games;
using MP.Core.History;
using MP.Scraping.Common.Configuration;
using MP.Scraping.Common.Constants;
using MP.Scraping.Common.Ftp;
using MP.Scraping.Common.Helpers;
using MP.Scraping.Models.History;
using MP.Scraping.Models.ServiceGames;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace MP.Scraping.Common.Extensions
{
    public static class GameExtension
    {
        public static void MergeGames(this Game mainGame, List<Game> games)
        {
            if (games?.Count == 0)
                return;

            foreach (Game game in games)
            {
                if (!CanMergeGame(mainGame, game))
                    continue;

                foreach (PriceInfo price in game.PriceInfos)
                    price.GameID = mainGame.ID;

                foreach (GameTagRelation tagRels in game.Tags)
                    if (!mainGame.Tags.Any(i => i.TagID == tagRels.TagID))
                        mainGame.Tags.Add(new GameTagRelation { GameID = mainGame.ID, TagID = tagRels.TagID });

                if (mainGame.Status == GameStatus.Completed)
                    game.Images.Clear();
                else
                    foreach (GImage img in game.Images)
                        img.GameID = mainGame.ID;

                foreach (GameRelationship child in game.Children.ToList())
                {
                    game.Children.Remove(child);
                    if (!mainGame.Children.Any(i => i.ChildID == child.ChildID))
                        mainGame.Children.Add(new GameRelationship { ParentID = mainGame.ID, ChildID = child.ChildID });
                }

                foreach (GameRelationship parent in game.Parents.ToList())
                {
                    game.Parents.Remove(parent);
                    if (!mainGame.Parents.Any(i => i.ParentID == parent.ParentID))
                        mainGame.Parents.Add(new GameRelationship { ChildID = mainGame.ID, ParentID = parent.ParentID });
                }

                foreach (GSystemRequirement sysReq in game.SystemRequirements.ToList())
                {
                    GSystemRequirement existingSysReq = mainGame.SystemRequirements
                        .FirstOrDefault(i => i.Type == sysReq.Type && i.SystemType == sysReq.SystemType);

                    if (existingSysReq == null)
                    {
                        game.SystemRequirements.Remove(sysReq);
                        mainGame.SystemRequirements.Add(sysReq);
                    }
                    else
                    {
                        VersionControl.ApplyChanges(existingSysReq, sysReq, ChangeOption.ApplyIfNull);
                    }
                }

                foreach(GTranslation trans in game.Translations.ToList())
                {
                    GTranslation existingTrans = mainGame.Translations
                        .FirstOrDefault(i => i.LanguageCode == trans.LanguageCode && i.Key == trans.Key);

                    if (existingTrans == null)
                    {
                        mainGame.Translations.Add(new GTranslation
                        {
                            GameID = mainGame.ID,
                            Key = trans.Key,
                            LanguageCode = trans.LanguageCode,
                            Value = trans.Value
                        });
                    }
                    else
                    {
                        VersionControl.ApplyChanges(existingTrans, trans, ChangeOption.ApplyIfNull);
                    }
                }

                if (mainGame.Languages == null && game.Languages != null)
                    mainGame.Languages = game.Languages;
                else if (game.Languages != null)
                    mainGame.MergeLocalizations(game.Languages);

                using (HistoryContext hContext = new HistoryContext())
                using (ServiceGameContext sgContext = new ServiceGameContext())
                {
                    foreach (string code in game.GameServicesCodes)
                    {
                        mainGame.AddServiceCode(code);

                        ServiceGame sGame = sgContext.Games.FirstOrDefault(i => i.ServiceCode == code && i.MainGameID == game.ID);
                        sGame.MainGameID = mainGame.ID;

                        List<Change> changes = hContext.Changes.Where(i => i.ServiceCode == code && i.GameID == game.ID).ToList();
                        changes.ForEach(i => i.GameID = mainGame.ID);
                    }
                    
                    sgContext.SaveChanges();
                    hContext.SaveChanges();
                }

                mainGame.GamePlatform |= game.GamePlatform;

                game.GameServicesCodes = null;
                game.Status = GameStatus.Deleted;
            }
        }

        public static void ResizeAndTransferImages(this Game game)
        {
            bool useFtp = ScrapingConfigurationManager.Config.FtpConfiguration.UseFtp;
            bool useResizer = ScrapingConfigurationManager.Config.ImageConfiguration.UseImageResizer;

            if (!useFtp && !useResizer)
                return;

            string imgsFolderPath = ScrapingConfigurationManager.Config.ImageConfiguration.ImageFolderPath;

            foreach (GImage image in game.Images)
            {
                string fileFullPath = Path.Combine(imgsFolderPath, image.Path);

                if (useResizer)
                    ResizeImage(image, imgsFolderPath, useFtp);
                else if (useFtp)
                    FtpManager.UploadFile(fileFullPath, image.Path);
            }
        }

        private static bool CanMergeGame(Game mainGame, Game game)
            => game.Status == GameStatus.New && !mainGame.GameServicesCodes.Intersect(game.GameServicesCodes).Any() && mainGame.GameType == game.GameType;

        private static void ResizeImage(GImage img, string imgsFolderPath, bool useFtp)
        {
            string fileFullPath = Path.Combine(imgsFolderPath, img.Path);

            if (!File.Exists(fileFullPath))
                return;

            Image imgFile = Image.FromFile(fileFullPath);


            switch (img.Tag)
            {
                case ImageTags.IMG_HORIZONTAL:
                    CreateNewImagesAndSave(ImageSizes.CoverVertical, false);
                    break;
                case ImageTags.IMG_VERTICAL:
                    CreateNewImagesAndSave(ImageSizes.CoverHorizontal, true);
                    break;
                case ImageTags.SCREENSHOT:
                    CreateNewImagesAndSave(ImageSizes.ScreenshotHorizontal, true);
                    break;
                default:
                    if (useFtp)
                        FtpManager.UploadFile(fileFullPath, img.Path);
                    break;
            }

            imgFile.Dispose();

            void CreateNewImagesAndSave(Dictionary<string, int> sizeMap, bool isHorizontalResize)
            {
                foreach(var size in sizeMap)
                {
                    int dotIndex = img.Path.LastIndexOf('.');
                    string newFilePath = img.Path.Insert(dotIndex, $"_{size.Key}");
                    Image newImgFile = (isHorizontalResize)
                        ? ImageHelper.ReduceImageByHorizontal(imgFile, size.Value)
                        : ImageHelper.ReduceImageByVertical(imgFile, size.Value);

                    string newFullImgPath = Path.Combine(imgsFolderPath, newFilePath);
                    newImgFile.Save(newFullImgPath, imgFile.RawFormat);
                    newImgFile.Dispose();

                    if (useFtp)
                        FtpManager.UploadFile(newFullImgPath, newFilePath);
                }
            }
        }
    }
}
