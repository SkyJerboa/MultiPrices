using MP.Core.Contexts.Games;
using MP.Core.History;
using MP.Scraping.Models.History;
using MP.Scraping.Models.ServiceGames;
using System.Collections.Generic;
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
                if (game.Status != GameStatus.New || mainGame.GameServicesCodes.Intersect(game.GameServicesCodes).Any() || mainGame.GameType != game.GameType)
                    continue;

                foreach (PriceInfo price in game.PriceInfos)
                    price.GameID = mainGame.ID;

                foreach (GImage img in game.Images)
                    img.GameID = mainGame.ID;

                foreach (GameTagRelation tagRels in game.Tags)
                    if (!mainGame.Tags.Any(i => i.TagID == tagRels.TagID))
                        mainGame.Tags.Add(new GameTagRelation { GameID = mainGame.ID, TagID = tagRels.TagID });

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
    }
}
