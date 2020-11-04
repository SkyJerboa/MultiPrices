using Dapper;
using Microsoft.EntityFrameworkCore;
using MP.Core.Contexts.Games;
using MP.Scraping.Models.History;
using MP.Scraping.Models.ServiceGames;
using Npgsql.Bulk;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace MP.Scraping.GameProcessing
{
    public class GameBulkSaver
    {
        private const int SAVE_GAME_COUNT = 1000;
        private const int SAVE_IMG_COUNT = 5000;
        private const int SAVE_PRICES_COUNT = 5000;
        private const int SAVE_RELEASE_DATE_COUNT = 3000;

        private int _gameCount;
        private int _imgCount;
        private int _piCount;
        private int __gamesWithReleaseDateChangeCount;

        private readonly string _serviceCode;

        private bool _canSaveImgs;

        private readonly List<int> _gameSubmittedIds = new List<int>();
        private readonly List<int> _returnedGamesIds = new List<int>();

        private readonly List<(Game, ServiceGame)> _gamesList = new List<(Game, ServiceGame)>();
        private readonly Dictionary<ServiceGame, Game> _sgToMGMap = new Dictionary<ServiceGame, Game>();
        private readonly List<PriceInfo> _piList = new List<PriceInfo>();
        private readonly List<(Game, ServiceGame)> _gamesWithReleaseDateChangeList = new List<(Game, ServiceGame)>();

        private readonly List<GImage> _mgImgList = new List<GImage>();
        private readonly List<SGImage> _sgImgList = new List<SGImage>();

        private readonly NpgsqlBulkUploader _mgContextBulk;
        private readonly NpgsqlBulkUploader _sgContextBulk;
        private readonly NpgsqlBulkUploader _hContextBulk;

        private readonly ServiceGameContext _sgContext;
        private readonly GameContext _mgContext;

        public GameBulkSaver(ServiceGameContext sgContext, GameContext gContext, HistoryContext hContext, Dictionary<ServiceGame, Game> sgToMGmap, string serviceCode)
        {
            _sgContext = sgContext;
            _mgContext = gContext;

            _mgContextBulk = new NpgsqlBulkUploader(gContext);
            _sgContextBulk = new NpgsqlBulkUploader(sgContext);
            _hContextBulk = new NpgsqlBulkUploader(hContext);

            _sgToMGMap = sgToMGmap;
            _serviceCode = serviceCode;
        }

        public void AddGames(Game mGame, ServiceGame sGame)
        {
            _gamesList.Add((mGame, sGame));
            _gameCount++;

            if (_gameCount >= SAVE_GAME_COUNT)
                SaveGamesData();
        }

        public void AddImages(List<SGImage> sgImgs, List<GImage> gImgs = null)
        {
            _sgImgList.AddRange(sgImgs);

            if(gImgs != null)
                _mgImgList.AddRange(gImgs);

            _imgCount += sgImgs.Count;

            if (_imgCount < SAVE_IMG_COUNT || !_canSaveImgs)
                return;

            SaveImages();
        }

        public void SaveGamesData()
        {
            SaveServiceGamesToDb();

            _gamesList.Clear();
            _gameCount = 0;
        }

        public void AllowSaveImages()
        {
            if (_imgCount >= SAVE_IMG_COUNT)
            {
                while (_imgCount >= SAVE_IMG_COUNT)
                {
                    List<GImage> gImgs = _mgImgList.Take(SAVE_IMG_COUNT).ToList();
                    gImgs.ForEach(i => i.GameID = i.Game.ID);
                    _mgImgList.RemoveRange(0, SAVE_IMG_COUNT);
                    List<SGImage> sgImgs = _sgImgList.Take(SAVE_IMG_COUNT).ToList();
                    sgImgs.ForEach(i => i.GameID = i.Game.ID);
                    _sgImgList.RemoveRange(0, SAVE_IMG_COUNT);


                    _mgContextBulk.Insert(gImgs);
                    _sgContextBulk.Insert(sgImgs);

                    _imgCount -= SAVE_IMG_COUNT;
                }
            }

            _canSaveImgs = true;
        }


        /// <summary>
        /// Сохраняет все данные в бд.
        /// Используйте метод только в конце обработки всех игр, чтобы досохранить данные.
        /// </summary>
        /// <returns>Количество удаленных игр</returns>
        public int SaveAllData(string countryCode, string currencyCode)
        {
            SaveGamesData();
            SaveImages();
            SavePriceInfos();
            SaveReleaseDate();

            List<PriceInfo> priceInfosToDelete = _mgContext.PriceInfos
                .AsNoTracking()
                .Where(i => i.ServiceCode == _serviceCode && i.CountryCode == countryCode 
                    && i.CurrencyCode == currencyCode && i.IsAvailable && !_gameSubmittedIds.Contains(i.GameID))
                .ToList();

            ReturnGames();
            ChangeGamesAvailability(priceInfosToDelete);

            return priceInfosToDelete.Count;
        }

        public void AddPriceInfo(PriceInfo priceInfo)
        {
            _piList.Add(priceInfo);
            _gameSubmittedIds.Add(priceInfo.Game.ID);
            if (!priceInfo.IsAvailable)
            {
                priceInfo.IsAvailable = true;
                _returnedGamesIds.Add(priceInfo.Game.ID);
            }

            _piCount++;

            if (_piCount >= SAVE_PRICES_COUNT)
                SavePriceInfos();
        }

        public void AddGamesToChangeReleaseDate(Game mainGame, ServiceGame serviceGame)
        {
            _gamesWithReleaseDateChangeList.Add((mainGame, serviceGame));

            __gamesWithReleaseDateChangeCount++;

            if (__gamesWithReleaseDateChangeCount >= SAVE_RELEASE_DATE_COUNT)
                SaveReleaseDate();
        }

        public void InsertSGRelations(IEnumerable<ServiceGameRelationship> sgRelations)
        {
            _sgContextBulk.Insert(sgRelations, InsertConflictAction.DoNothing());
        }

        public void DeleteSGRelations(IEnumerable<ServiceGameRelationship> sgRelations)
        {
            if (!sgRelations.Any())
                return;

            StringBuilder sb = new StringBuilder();
            foreach (ServiceGameRelationship sgr in sgRelations)
                sb.Append($"({sgr.Parent.ID},{sgr.Child.ID}),");

            sb.Remove(sb.Length - 1, 1);

            //временное решение, потом стоит переписать
            string query = $@"DELETE FROM ""{_sgContext.Model.FindEntityType(typeof(ServiceGameRelationship)).GetTableName()}"" WHERE (""ParentID"",""ChildID"") in ({sb})";

            IDbConnection dbConnection = _sgContext.Database.GetDbConnection();
            if (dbConnection.State != ConnectionState.Open)
            {
                dbConnection.Open();
                dbConnection.Execute(query);
                dbConnection.Close();
            }
            else
            {
                dbConnection.Execute(query);
            }
        }

        public void InsertGRelatios(IEnumerable<GameRelationship> gRelations)
        {
            _mgContextBulk.Insert(gRelations, InsertConflictAction.DoNothing());
        }

        public void SaveHistory(IEnumerable<RelationChange> relationChanges, IEnumerable<Change> changes)
        {
            _hContextBulk.Insert(relationChanges, InsertConflictAction.DoNothing());

            _hContextBulk.Insert(changes.Where(i => i.ID == 0));
            _hContextBulk.Update(changes.Where(i => i.ID != 0).OrderBy(i => i.ID));
        }

        private void SaveImages()
        {
            if (!_canSaveImgs || _mgImgList.Count == 0 && _sgImgList.Count == 0)
                return;

            _mgImgList.ForEach(i => i.GameID = i.Game.ID);
            _sgImgList.ForEach(i => i.GameID = i.Game.ID);

            _mgContextBulk.Insert(_mgImgList);
            _sgContextBulk.Insert(_sgImgList);

            _mgImgList.Clear();
            _sgImgList.Clear();

            _imgCount = 0;
        }

        private void SaveServiceGamesToDb()
        {
            if (_gamesList.Count == 0)
                return;

            SaveMainGamesToDb();

            List<ServiceGame> serviceGamesList = _gamesList.Select(i => i.Item2).ToList();

            serviceGamesList.ForEach(i => i.MainGameID = _sgToMGMap[i].ID);
            _sgContextBulk.Insert(serviceGamesList.Where(i => i.ID == 0).ToList());
            _sgContextBulk.Update(serviceGamesList.Where(i => i.ID != 0).OrderBy(i => i.ID).ToList());

            List<SGSystemRequirement> systemRequirements = new List<SGSystemRequirement>();
            List<SGTranslation> translations = new List<SGTranslation>();

            foreach (ServiceGame g in serviceGamesList)
            {
                int id = g.ID;

                g.SystemRequirements.ToList().ForEach(i => i.GameID = id);
                systemRequirements.AddRange(g.SystemRequirements);

                g.Translations.ToList().ForEach(i => i.GameID = id);
                translations.AddRange(g.Translations);
            }

            _sgContextBulk.Insert(systemRequirements.Where(i => i.ID == 0).ToList());
            _sgContextBulk.Update(systemRequirements.Where(i => i.ID != 0).OrderBy(i => i.ID).ToList());
            _sgContextBulk.Insert(translations.Where(i => i.ID == 0).ToList());
            _sgContextBulk.Update(translations.Where(i => i.ID != 0).OrderBy(i => i.ID).ToList());

            _gameSubmittedIds.AddRange(serviceGamesList.Select(i => i.MainGameID));
        }

        private void SaveMainGamesToDb()
        {
            List<Game> mainGames = _gamesList.Select(i => i.Item1).ToList();

            mainGames.OrderBy(i => i.ID);
            _mgContextBulk.Insert(mainGames.Where(i => i.ID == 0).ToList());
            _mgContextBulk.Update(mainGames.Where(i => i.ID != 0).OrderBy(i => i.ID).ToList());

            List<GSystemRequirement> systemRequirements = new List<GSystemRequirement>();
            List<GTranslation> translations = new List<GTranslation>();
            List<PriceInfo> priceInfos = new List<PriceInfo>();
            List<GameTagRelation> tagRelations = new List<GameTagRelation>();
            HashSet<Tag> tags = new HashSet<Tag>();

            foreach (Game g in mainGames)
            {
                int id = g.ID;

                g.SystemRequirements.ToList().ForEach(i => i.GameID = id);
                systemRequirements.AddRange(g.SystemRequirements);

                g.Translations.ToList().ForEach(i => i.GameID = id);
                translations.AddRange(g.Translations);

                g.PriceInfos.ToList().ForEach(i => i.GameID = id);
                priceInfos.AddRange(g.PriceInfos);

                g.Tags.ToList().ForEach(i => i.GameID = id);
                tagRelations.AddRange(g.Tags);

                tags.UnionWith(g.Tags.Where(i => i.Tag.ID == 0).Select(i => i.Tag));
            }

            string translationsKeyColumns = String.Join(",", new string[] 
            { 
                $"\"{nameof(GTranslation.LanguageCode)}\"",
                $"\"{nameof(GTranslation.GameID)}\"",
                $"\"{nameof(GTranslation.Key)}\""
            });

            _mgContextBulk.Insert(systemRequirements.Where(i => i.ID == 0).ToList());
            _mgContextBulk.Update(systemRequirements.Where(i => i.ID != 0).OrderBy(i => i.ID).ToList());
            _mgContextBulk.Insert(translations, InsertConflictAction.UpdateIndex<GTranslation>(translationsKeyColumns, i => i.Value));

            _mgContextBulk.Insert(priceInfos.Where(i => i.ID == 0).ToList());
            _mgContextBulk.Update(priceInfos.Where(i => i.ID != 0).OrderBy(i => i.ID).ToList());

            List<Price> prices = new List<Price>();
            foreach (PriceInfo pi in priceInfos)
            {
                pi.Prices.ToList().ForEach(i => i.ServicePriceID = pi.ID);
                prices.AddRange(pi.Prices.Where(i => i.ID == 0));
            }
            _mgContextBulk.Insert(prices);

            List<Tag> tagList = tags.ToList();
            _mgContextBulk.Insert(tagList.Where(i => i.ID == 0).ToList());
            _mgContextBulk.Update(tagList.Where(i => i.ID != 0).OrderBy(i => i.ID).ToList());

            tagRelations.ForEach(i => i.TagID = i.Tag.ID);
            _mgContextBulk.Insert(tagRelations, InsertConflictAction.DoNothing());
        }

        private void SavePriceInfos()
        {
            List<Price> pList = _piList
                .Select(i => i.GetLastPrice())
                .Where(i => i?.ID == 0)
                .ToList();
            _piList.ForEach(i => i.GameID = i.Game.ID);

            _mgContextBulk.Insert(_piList.Where(i => i.ID == 0).ToList());
            _mgContextBulk.Update(_piList.Where(i => i.ID != 0).OrderBy(i => i.ID).ToList());

            pList.ForEach(i => i.ServicePriceID = i.PriceInfo.ID);
            _mgContextBulk.Insert(pList);

            _piCount = 0;
        }

        private void SaveReleaseDate()
        {
            List<Game> mainGames = new List<Game>();
            List<ServiceGame> serviceGames = new List<ServiceGame>();

            foreach(var games in _gamesWithReleaseDateChangeList)
            {
                mainGames.Add(games.Item1);
                serviceGames.Add(games.Item2);
            }

            _mgContextBulk.Update(mainGames.OrderBy(i => i.ID).ToList());
            _sgContextBulk.Update(serviceGames.OrderBy(i => i.ID).ToList());

            _gamesWithReleaseDateChangeList.Clear();
            __gamesWithReleaseDateChangeCount = 0;
        }

        private void ReturnGames()
        {
            if (_returnedGamesIds.Count == 0)
                return;

            List<Game> mainGamesToReturn = _mgContext.Games
                .AsNoTracking()
                .Where(i => i.Status == GameStatus.NotAvailable && _returnedGamesIds.Contains(i.ID))
                .ToList();

            List<ServiceGame> serviceGamesToReturn = _sgContext.Games
                .AsNoTracking()
                .Where(i => i.Status == ServiceGameStatus.Deleted && i.ServiceCode == _serviceCode && _returnedGamesIds.Contains(i.MainGameID))
                .ToList();

            mainGamesToReturn.ForEach(i => i.Status = GameStatus.Returned);
            serviceGamesToReturn.ForEach(i => i.Status = ServiceGameStatus.InfoUpdated);

            _mgContextBulk.Update(mainGamesToReturn.OrderBy(i => i.ID).ToList());
            _sgContextBulk.Update(serviceGamesToReturn.OrderBy(i => i.ID).ToList());
        }

        private void ChangeGamesAvailability(List<PriceInfo> priceInfosToDelete)
        {
            if (priceInfosToDelete.Count == 0)
                return;

            priceInfosToDelete.ForEach(i => i.IsAvailable = false);

            _mgContextBulk.Update(priceInfosToDelete.OrderBy(i => i.ID).ToList());

            
            int[] mainGameIdsOfDeletedPI = priceInfosToDelete.Select(i => i.GameID).ToArray();

            List<PriceInfo> allPIsOfEditingGames = _mgContext.PriceInfos
                .AsNoTracking()
                .Where(i => mainGameIdsOfDeletedPI.Contains(i.GameID))
                .ToList();

            List<int> mgToDeleteIDs = new List<int>();
            List<int> sgToDeleteMGIDs = new List<int>();

            foreach(var piGameGrop in allPIsOfEditingGames.GroupBy(i => i.GameID))
            {
                if (piGameGrop.All(i => !i.IsAvailable))
                {
                    mgToDeleteIDs.Add(piGameGrop.Key);
                    sgToDeleteMGIDs.Add(piGameGrop.Key);
                }
                else if (piGameGrop.Where(i => i.ServiceCode == _serviceCode).All(i => !i.IsAvailable))
                {
                    sgToDeleteMGIDs.Add(piGameGrop.Key);
                }
            }

            List<Game> gamesWithNotAvailableStatus = _mgContext.Games
                .AsNoTracking()
                .Where(i => i.Status != GameStatus.NotAvailable && mgToDeleteIDs.Contains(i.ID))
                .ToList();
            List<ServiceGame> deletedServiceGames = _sgContext.Games
                .AsNoTracking()
                .Where(i => i.ServiceCode == _serviceCode && i.Status != ServiceGameStatus.Deleted 
                    && sgToDeleteMGIDs.Contains(i.MainGameID))
                .ToList();

            gamesWithNotAvailableStatus.ForEach(i => i.Status = GameStatus.NotAvailable);
            deletedServiceGames.ForEach(i => i.Status = ServiceGameStatus.Deleted);

            
            _mgContextBulk.Update(gamesWithNotAvailableStatus.OrderBy(i => i.ID).ToList());
            _sgContextBulk.Update(deletedServiceGames.OrderBy(i => i.ID).ToList());
        }
    }
}
