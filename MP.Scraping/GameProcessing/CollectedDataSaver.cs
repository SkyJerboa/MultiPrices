using Microsoft.EntityFrameworkCore;
using MP.Core;
using MP.Core.Common.Heplers;
using MP.Core.Contexts.Games;
using MP.Core.History;
using MP.Scraping.Common;
using MP.Scraping.Common.Configuration;
using MP.Scraping.Common.Helpers;
using MP.Scraping.GameProcessing.ScrapedGameModels;
using MP.Scraping.Models.History;
using MP.Scraping.Models.ServiceGames;
using MP.Scraping.Models.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TK = MP.Core.Common.Constants.TransKeys;

namespace MP.Scraping.GameProcessing
{
    public class CollectedDataSaver : IDisposable
    {
        public delegate void GameSavedHandler();
        public event GameSavedHandler GamesSaved;

        private readonly HistoryContext _historyContext = new HistoryContext();
        private readonly ServiceGameContext _serviceGameContext = new ServiceGameContext();
        private readonly GameContext _mainGameContext 
            = new GameContext(ScrapingConfigurationManager.Config.SiteConnection);

        private readonly CancellationToken _cancellationToken;

        private readonly ServiceRequestOptions _serviceRequestOptions;
        private readonly ScrapedGameCollection _collectedGames;
        private readonly ServiceRequest _requestsSumInfo;
        private readonly string _serviceCode;

        private readonly IQueryable<ServiceGame> _serviceGamesQuery;
        private readonly IQueryable<Game> _mainGamesQuery;
        
        private readonly GameBulkSaver _gameSaver;
        private readonly ImageDownloader _imgDownloader;
        
        private readonly List<Tag> _tagsInDB;
        private readonly List<Change> _changesList = new List<Change>();
        private readonly Dictionary<ServiceGame, Game> _serviceGameToMainGameMap = new Dictionary<ServiceGame, Game>();
        private readonly Dictionary<string, string> _gameServiceToSiteTagsMap;
        private readonly Dictionary<string, ServiceGame> _innerIdToServiceGameMap = new Dictionary<string, ServiceGame>();
        private readonly HashSet<string> _allNameIds;
        private readonly HashSet<string> _newAddedNameIds = new HashSet<string>();
        private readonly HashSet<KeyValuePair<string, string>> _serviceGameRelations = new HashSet<KeyValuePair<string, string>>();

        public CollectedDataSaver(string serviceCode, ScrapedGameCollection collectedGames, ServiceRequest requestsSumInfo, ServiceRequestOptions options, CancellationToken token)
        {
            _serviceCode = serviceCode;
            _collectedGames = collectedGames;
            _requestsSumInfo = requestsSumInfo;
            _serviceRequestOptions = options;
            _cancellationToken = token;

            _serviceGamesQuery = _serviceGameContext.GetServiceGamesWithoutTracking(_serviceCode);
            _mainGamesQuery = CreateMainGamesQuerie();

            _gameSaver = new GameBulkSaver(
                sgContext: _serviceGameContext, 
                gContext: _mainGameContext, 
                hContext: _historyContext, 
                sgToMGmap: _serviceGameToMainGameMap, 
                serviceCode: _serviceCode);
            _tagsInDB = _mainGameContext.Tags.ToList();
            _gameServiceToSiteTagsMap = _serviceGameContext.TagsMap
                .AsNoTracking()
                .ToDictionary(k => k.SourceTag, v => v.MainTag);
            _allNameIds = _mainGameContext.Games
                .AsNoTracking()
                .Select(i => i.NameID)
                .ToHashSet();

            if (!options.IsTesting)
            {
                string imgServerDirectory = ScrapingConfigurationManager.Config.ImageFolderPath;
                _imgDownloader = new ImageDownloader(serviceCode, imgServerDirectory, _gameSaver, _cancellationToken);
            }
        }

        private IQueryable<Game> CreateMainGamesQuerie()
        {
            return _mainGameContext.Games
               .AsNoTracking()
               .Include(i => i.Children)
                   .ThenInclude(i => i.Child)
               .Include(i => i.PriceInfos)
                   .ThenInclude(i => i.Prices)
               .Include(i => i.Tags)
                   .ThenInclude(i => i.Tag)
               .Include(i => i.Images)
               .Include(i => i.SystemRequirements)
               .Include(i => i.Translations);
        }

        public void SaveData()
        {
            foreach(ScrapedGame collectedGame in _collectedGames)
            {
                if (_cancellationToken.IsCancellationRequested)
                    CancelSave();

                ServiceGame existingServiceGame = _serviceGamesQuery.FirstOrDefault(i => i.InnerID == collectedGame.InnerID);

                if (IsOnlyPriceUpdate(collectedGame))
                {
                    UpdatePriceIfNecessary(existingServiceGame, collectedGame);
                    continue;
                }

                UpdateFullGame(existingServiceGame, collectedGame);
            }

            SaveAllDataAndWaitImgDownloading();
        }

        private void CancelSave()
        {
            Dispose();
            throw new OperationCanceledException();
        }

        private bool IsOnlyPriceUpdate(ScrapedGame game) => _serviceRequestOptions.IsOnlyPrice && !game.IsNewGame;

        private void UpdatePriceIfNecessary(ServiceGame existingServiceGame, ScrapedGame collectedGame)
        {
            Game existingMainGame = GetMainGameWithPrices(existingServiceGame.MainGameID);
            PriceInfo existingPI = GetPriceInfoFromGame(existingMainGame);
            PriceInfo newPI = CreatePriceInfo(existingMainGame, collectedGame.Offer);
            PriceInfo finalPI = existingPI;

            bool hasPIChanges = true;
            bool hasAdditionalChanges = false;
            if (existingPI == null)
            {
                finalPI = existingPI;
            }
            else
            {
                hasPIChanges = CheckAndChangePriceInfo(finalPI, newPI);
                hasAdditionalChanges |= CheckAndChangePreorder(finalPI, collectedGame.Offer);
                hasAdditionalChanges |= !finalPI.IsAvailable;
            }

            hasAdditionalChanges |= ChackAndChangePreorderReleaseDate(existingServiceGame, existingMainGame, collectedGame.ReleaseDate);

            UpdateServiceGameStatusAfterPriceUpdate(hasAdditionalChanges, hasPIChanges);

            _gameSaver.AddPriceInfo(finalPI);
            _innerIdToServiceGameMap.Add(existingServiceGame.InnerID, existingServiceGame);
        }

        private void UpdateServiceGameStatusAfterPriceUpdate(bool hasAdditionalChanges, bool hasPriceChanges)
        {
            if (hasAdditionalChanges)
            {
                _requestsSumInfo.Updated++;
            }
            else
            {
                if (hasPriceChanges)
                    _requestsSumInfo.PriceUpdated++;
                else
                    _requestsSumInfo.NoChanged++;
            }
        }

        private void UpdateFullGame(ServiceGame existingServiceGame, ScrapedGame collectedGame)
        {
            ServiceGame newServiceGame, finalServiceGame;
            Game existingMainGame, newMainGame, finalMainGame;

            var gamesInstances = CreateNewGamesInstance(collectedGame);
            newServiceGame = finalServiceGame = gamesInstances.Item1;
            newMainGame = finalMainGame = gamesInstances.Item2;

            int updatedPriceCounter = _requestsSumInfo.PriceUpdated;
            bool hasGameChanges = false;


            bool isServiceGameExistsInDB = existingServiceGame != null;

            if (isServiceGameExistsInDB)
            {
                existingMainGame = _mainGamesQuery.FirstOrDefault(i => i.ID == existingServiceGame.MainGameID);

                List<Change> serviceGameChanges = GetServiceGameChangesAndApplyIfNull(existingServiceGame, newServiceGame);

                finalServiceGame = existingServiceGame;

                PriceInfo newPriceInfo = CreatePriceInfo(newMainGame, collectedGame.Offer);
                AddOrChangePriceInfo(existingMainGame, newPriceInfo);

                if (serviceGameChanges.Count > 0)
                {
                    hasGameChanges = true;
                    _changesList.AddRange(serviceGameChanges);
                    existingServiceGame.Status = ServiceGameStatus.InfoUpdated;
                }
                
                if (existingServiceGame.Status == ServiceGameStatus.Deleted)
                    existingServiceGame.Status = ServiceGameStatus.InfoUpdated;
            }
            else
            {
                existingMainGame = _mainGamesQuery
                    .FirstOrDefault(i => i.NameID == newMainGame.NameID && i.GameType == newMainGame.GameType);
                if (IsMainGameHasNewServiceGame(existingMainGame))
                    existingMainGame = null;

                if (existingMainGame != null)
                    existingMainGame.PriceInfos.Add(CreatePriceInfo(newMainGame, collectedGame.Offer));
                else
                    newMainGame.NameID = ChangeDublicateNameId(newMainGame.NameID);


                _newAddedNameIds.Add(newMainGame.NameID);

                if (!_serviceRequestOptions.IsTesting)
                    _imgDownloader.AddNewDownloadingImgTask(collectedGame.Images, newServiceGame, existingMainGame ?? newMainGame);
            }


            bool isMainGameExistsInDB = existingMainGame != null;

            if (existingMainGame == null)
            {
                finalMainGame.PriceInfos.Add(CreatePriceInfo(newMainGame, collectedGame.Offer));
                _allNameIds.Add(finalMainGame.NameID);
            }
            else
            {
                Change change = new Change(existingMainGame, newMainGame, _serviceCode, existingMainGame.ID, ChangeOption.ExcludeNull);
                bool additionalChanges = AddNewAdditionalDataToExistingMainGame(existingMainGame, newMainGame);
                if (change.HasChanges || additionalChanges)
                {
                    if (change.HasChanges)
                    {
                        hasGameChanges = true;
                        _changesList.Add(change);
                    }
                    if (finalServiceGame.Status != ServiceGameStatus.InfoUpdated)
                        finalServiceGame.Status = ServiceGameStatus.InfoUpdated;
                }

                finalMainGame = existingMainGame;

                finalMainGame.MergeLocalizations(newMainGame.Languages);
                ChangeStatusAndAddServiceCodeToExistingMainGame(finalMainGame);
            }

            CreateNewTagsRelations(collectedGame.Tags, finalMainGame);

            _innerIdToServiceGameMap.Add(finalServiceGame.InnerID, finalServiceGame);
            _serviceGameToMainGameMap.Add(finalServiceGame, finalMainGame);

            bool priceWasUpdated = _requestsSumInfo.PriceUpdated > updatedPriceCounter;
            WriteStatusCountToRequest(priceWasUpdated, !isServiceGameExistsInDB, hasGameChanges);

            if (collectedGame.ChildrenIDs != null && collectedGame.ChildrenIDs.Length > 0)
            {
                var currentServiceGameRelations = collectedGame.ChildrenIDs
                    .Select(i => new KeyValuePair<string, string>(finalServiceGame.InnerID, i));
                _serviceGameRelations.UnionWith(currentServiceGameRelations);
            }

            _gameSaver.AddGames(finalMainGame, finalServiceGame);
        }

        //при переходе на net core 5.0 нужно переписать метод так, чтобы он брал только необходимые PriceInfo с учетом сераиса и страны
        private Game GetMainGameWithPrices(int gameId)
        {
            return _mainGameContext.Games
                .AsNoTracking()
                .Include(i => i.PriceInfos)
                    .ThenInclude(i => i.Prices)
                .FirstOrDefault(i => i.ID == gameId);
        }

        private PriceInfo GetPriceInfoFromGame(Game mainGame) 
        {
            return mainGame.PriceInfos
                .FirstOrDefault(i => i.ServiceCode == _serviceCode && i.CountryCode == _serviceRequestOptions.CountryCode
                    && i.CurrencyCode == _serviceRequestOptions.CurrencyCode);
        }

        private PriceInfo CreatePriceInfo(Game game, ScrapedGameOffer gameOffer)
        {
            PriceInfo pInfo = new PriceInfo
            {
                Game = game,
                CountryCode = _serviceRequestOptions.CountryCode,
                ServiceCode = _serviceCode,
                CurrencyCode = _serviceRequestOptions.CurrencyCode,
                GameLink = gameOffer.URL,
                FullPrice = gameOffer.FullPrice,
                CurrentPrice = gameOffer.CurrentPrice,
                Discount = gameOffer.Discount,
                IsPreorder = gameOffer.IsPreorder
            };

            pInfo.IsFree = (pInfo.FullPrice == 0);

            if (pInfo.CurrentPrice != null)
            {
                Price price = CreatePrice(pInfo);
                pInfo.Prices.Add(price);
            }

            return pInfo;
        }

        private Price CreatePrice(PriceInfo priceInfo)
        {
            Price price = new Price
            {
                PriceInfo = priceInfo,
                ChangingDate = DateTime.Now,
                CurrentPrice = priceInfo.CurrentPrice,
                Discount = priceInfo.Discount
            };

            return price;
        }

        private bool CheckAndChangePriceInfo(PriceInfo changedPI, PriceInfo comparedPI)
        {
            bool hasChanges = false;

            if ((changedPI.CurrentPrice == null && comparedPI.CurrentPrice != null) || changedPI.HasPriceChanges(comparedPI))
            {
                changedPI.UpdateOnlyPrice(comparedPI);
                changedPI.Prices.Add(CreatePrice(changedPI));
                hasChanges = true;
            }

            return hasChanges;
        }

        private bool CheckAndChangePreorder(PriceInfo checkedPriceInfo, ScrapedGameOffer offer)
        {
            if (checkedPriceInfo.IsPreorder == offer.IsPreorder)
                return false;

            checkedPriceInfo.IsPreorder = offer.IsPreorder;
            return true;
        }

        private bool ChackAndChangePreorderReleaseDate(ServiceGame serviceGame, Game mainGame, object date)
        {
            bool isChanged = false;

            if (date is DateTime)
            {
                DateTime releaseDate = ((DateTime)date).Date;
                if (IsReleaseDateChanged(releaseDate))
                {
                    mainGame.ReleaseDate = serviceGame.AvailabilityDate = releaseDate;
                    mainGame.LastModifyDate = DateTime.Now;
                    _gameSaver.AddGamesToChangeReleaseDate(mainGame, serviceGame);
                    isChanged = true;
                }
            }

            return isChanged;

            bool IsReleaseDateChanged(DateTime releaseDate) =>
                mainGame.ReleaseDate >= DateTime.Now.Date && serviceGame.AvailabilityDate != releaseDate
                    && mainGame.ReleaseDate != releaseDate;
        }

        private (ServiceGame, Game) CreateNewGamesInstance(ScrapedGame scrapedGame)
        {
            string name = scrapedGame.Name.ClearName();

            ServiceGame game = new ServiceGame
            {
                Name = name,
                ServiceCode = _serviceCode,
                InnerID = scrapedGame.InnerID,
                OfferID = scrapedGame.OfferID,
                Platforms = scrapedGame.Platforms ?? GamePlatform.Unknown,
                ImagesPath = scrapedGame.NameID,
                Status = ServiceGameStatus.New,
                IsAvailable = scrapedGame.IsAvailable,
                LastModifyDate = DateTime.Now,
                Languages = scrapedGame.LocalizationString.ToPostgreJsonFormat(),
                Translations = new List<SGTranslation>(),
                SystemRequirements = scrapedGame.SystemRequirements?.ToList() ?? new List<SGSystemRequirement>(),
                Images = new List<SGImage>()
            };
            game.Translations.Add(CreateServiceGameTranslate(game, TK.GAME_NAME, scrapedGame.LocalizedName ?? name));
            if (scrapedGame.Description != null)
                game.Translations.Add(CreateServiceGameTranslate(game, TK.GAME_DESCRIPTION, scrapedGame.Description.ClearHtmlText()));
            object availability = scrapedGame.ReleaseDate;
            if (availability is DateTime)
                game.AvailabilityDate = (DateTime)availability;
            else
                game.AvailabilityString = availability as string;


            Game mainGame = new Game
            {
                Name = name,
                NameID = scrapedGame.NameID,
                GameType = scrapedGame.GameType ?? GameEntityType.Unknown,
                GamePlatform = scrapedGame.Platforms ?? GamePlatform.Unknown,
                Publisher = scrapedGame.Publisher,
                Developer = scrapedGame.Developer,
                Brand = scrapedGame.Brand,
                ImagesPath = game.ImagesPath,
                ReleaseDate = game.AvailabilityDate,
                LastModifyDate = DateTime.Now,
                Order = 1,
                Languages = game.Languages,
                Translations = new List<GTranslation>(),
                SystemRequirements = new List<GSystemRequirement>(),
                Images = new List<GImage>(),
                GameServicesCodes = new string[] { _serviceCode },
                Status = GameStatus.New
            };
            foreach (SGTranslation trans in game.Translations)
                mainGame.Translations.Add(CreateMainGameTranslate(mainGame, trans.Key, trans.Value));
            foreach (SGSystemRequirement sysReq in game.SystemRequirements)
                mainGame.SystemRequirements.Add(sysReq.CreateCopy<GSystemRequirement>());


            return (game, mainGame);
        }

        private SGTranslation CreateServiceGameTranslate(ServiceGame serviceGame, string key, string value)
        {
            SGTranslation trans = new SGTranslation
            {
                Game = serviceGame,
                LanguageCode = _serviceRequestOptions.LanguageCode,
                Key = key,
                Value = value
            };

            return trans;
        }

        private GTranslation CreateMainGameTranslate(Game mainGame, string key, string value)
        {
            GTranslation trans = new GTranslation
            {
                Game = mainGame,
                LanguageCode = _serviceRequestOptions.LanguageCode,
                Key = key,
                Value = value
            };

            return trans;
        }

        private List<Change> GetServiceGameChangesAndApplyIfNull(ServiceGame existingGame, ServiceGame comparedGame)
        {
            List<Change> changes = new List<Change>();

            Change change = new Change(existingGame, comparedGame, _serviceCode, existingGame.MainGameID, ChangeOption.ExcludeNull);
            if (change.HasChanges)
                changes.Add(change);

            var comparedTranslations = comparedGame.Translations.Where(i => i.LanguageCode == _serviceRequestOptions.LanguageCode);
            foreach (SGTranslation trans in comparedTranslations)
            {
                SGTranslation existingTrans = existingGame.Translations
                    .FirstOrDefault(i => i.LanguageCode == _serviceRequestOptions.LanguageCode && i.Key == trans.Key);

                if (existingTrans != null)
                {
                    change = new Change(existingTrans, trans, _serviceCode, existingGame.MainGameID, ChangeOption.ExcludeNull);
                    if (change.HasChanges)
                        changes.Add(change);
                }
                else
                {
                    trans.Game = existingGame;
                    existingGame.Translations.Add(trans);
                }
            }

            var systemRequirements = comparedGame.SystemRequirements ?? Enumerable.Empty<SGSystemRequirement>();
            foreach (SGSystemRequirement sysReq in systemRequirements)
            {
                SGSystemRequirement existingSr = existingGame.SystemRequirements?
                    .FirstOrDefault(i => i.Type == sysReq.Type && i.SystemType == sysReq.SystemType);

                if (existingSr != null)
                {
                    change = new Change(existingSr, sysReq, _serviceCode, existingGame.MainGameID, ChangeOption.ExcludeNull);
                    if (change.HasChanges)
                        changes.Add(change);
                }
                else
                {
                    sysReq.Game = existingGame;
                    existingGame.SystemRequirements.Add(sysReq);
                }
            }

            return changes;
        }

        private void AddOrChangePriceInfo(Game game, PriceInfo comparedPI)
        {
            PriceInfo existingPI = game.PriceInfos?
                .FirstOrDefault(i => i.ServiceCode == _serviceCode && i.CountryCode == _serviceRequestOptions.CountryCode 
                    && i.CurrencyCode == _serviceRequestOptions.CurrencyCode);

            if (existingPI != null)
            {
                bool hasPriceChanges = CheckAndChangePriceInfo(existingPI, comparedPI);
                //можно при изменении PreOrder записывать изменения в InfoUpdated вместо PriceUpdated
                existingPI.IsPreorder = comparedPI.IsPreorder;

                if (!existingPI.IsAvailable)
                {
                    existingPI.IsAvailable = true;
                    _requestsSumInfo.Returned++;
                }
                else if (hasPriceChanges)
                {
                    _requestsSumInfo.PriceUpdated++;
                }
            }
            else
            {
                game.PriceInfos.Add(comparedPI);
            }
        }

        private string ChangeDublicateNameId(string nameId)
        {
            if (!_allNameIds.Contains(nameId))
                return nameId;

            int i = 1;
            string newNameId = nameId;
            while (_allNameIds.Contains(newNameId))
            {
                newNameId = $"{nameId}_c{i}";
                i++;
            }

            return newNameId;
        }

        private bool IsMainGameHasNewServiceGame(Game game)
        {
            if (game == null)
                return false;

            return game.GameServicesCodes != null && game.GameServicesCodes.Contains(_serviceCode)
                || _newAddedNameIds.Contains(game.NameID);
        }

        private bool AddNewAdditionalDataToExistingMainGame(Game existingGame, Game newGame)
        {
            bool hasChanges = false;

            foreach (GTranslation trans in newGame.Translations)
            {
                GTranslation existingTrans
                   = existingGame.Translations.FirstOrDefault(i => i.LanguageCode == trans.LanguageCode && i.Key == trans.Key);

                if (existingTrans == null)
                {
                    trans.Game = existingGame;
                    existingGame.Translations.Add(trans);
                    hasChanges = true;
                }
            }


            foreach (GSystemRequirement sysReq in newGame.SystemRequirements)
            {
                GSystemRequirement existingSysReq = existingGame.SystemRequirements
                    .FirstOrDefault(i => i.Type == sysReq.Type && i.SystemType == sysReq.SystemType);

                if (existingSysReq == null)
                {
                    sysReq.Game = existingGame;
                    existingGame.SystemRequirements.Add(sysReq);
                    hasChanges = true;
                }
            }

            return hasChanges;
        }

        private void ChangeStatusAndAddServiceCodeToExistingMainGame(Game mainGame)
        {
            if (mainGame.Status == GameStatus.NotAvailable)
                mainGame.Status = GameStatus.Returned;
            if (!mainGame.GameServicesCodes.Contains(_serviceCode))
                mainGame.AddServiceCode(_serviceCode);
        }

        private List<GameTagRelation> CreateNewTagsRelations(string[] collectedTags, Game mGame)
        {
            if (collectedTags == null || collectedTags.Length == 0)
                return new List<GameTagRelation>();

            List<GameTagRelation> newTagRalations = new List<GameTagRelation>();
            List<string> existingTags = mGame.Tags?.Select(i => i.Tag.Name).ToList();
            List<string> currTags = new List<string>();

            foreach (string tag in collectedTags)
            {
                if (tag.IsNullOrEmpty())
                    continue;

                string tagName = tag.CreateOneLineString();

                if (_gameServiceToSiteTagsMap.ContainsKey(tagName))
                    tagName = _gameServiceToSiteTagsMap[tagName];

                if (IsCurrentTagExists(tagName))
                    continue;

                GameTagRelation gtr;
                Tag existingTag = _tagsInDB.FirstOrDefault(i => i.Name == tagName);
                if (existingTag == null)
                {
                    Tag newTag = new Tag { Name = tagName };
                    _tagsInDB.Add(newTag);
                    gtr = new GameTagRelation { Tag = newTag, Game = mGame };
                }
                else
                {
                    gtr = new GameTagRelation { Tag = existingTag, Game = mGame };
                }

                mGame.Tags.Add(gtr);
                currTags.Add(tagName);
                newTagRalations.Add(gtr);
            }

            return newTagRalations;

            bool IsCurrentTagExists(string tagName)
                => tagName.IsNullOrEmpty() || existingTags != null 
                    && existingTags.Contains(tagName) || currTags.Contains(tagName);
        }

        private void WriteStatusCountToRequest(bool priceWasUpdated, bool isNewGame, bool hasGameChange)
        {
            if (isNewGame)
            {
                _requestsSumInfo.New++;
            }
            else
            {
                if (!hasGameChange)
                {
                    if (!priceWasUpdated)
                        _requestsSumInfo.NoChanged++;
                }
                else
                {
                    _requestsSumInfo.Updated++;
                }
            }
        }

        private void SaveAllDataAndWaitImgDownloading()
        {
            _gameSaver.SaveGamesData();

            List<RelationChange> relChanges = UpdateGamesRelations();
            List<Change> updatedChanges = UpdateHistory();

            _gameSaver.SaveHistory(relChanges, updatedChanges);

            _gameSaver.AllowSaveImages();

            GamesSaved?.Invoke();

            _imgDownloader?.WaitLastTask();
            _requestsSumInfo.Deleted = _gameSaver.SaveAllData(_serviceRequestOptions.CountryCode, _serviceRequestOptions.CurrencyCode);

            _imgDownloader?.DeleteTempFile();
        }

        private List<RelationChange> UpdateGamesRelations()
        {
            //получаем список всех связей, хранящихся в таблице
            List<ServiceGameRelationship> sgRelationsInDB = _serviceGameContext.GameRelationships
                .AsNoTracking()
                .Where(i => i.ServiceCode == _serviceCode)
                .Include(i => i.Child)
                .Include(i => i.Parent)
                .ToList();
            //преобразуем полученный список связей в HashSet
            HashSet<KeyValuePair<ServiceGame, ServiceGame>> sgRelationsInDBHashSet = sgRelationsInDB
                .Select(i => new KeyValuePair<ServiceGame, ServiceGame>(i.Parent, i.Child))
                .ToHashSet();
            //создаем HashSet из всех связей текущего запроса
            HashSet<KeyValuePair<ServiceGame, ServiceGame>> currentRequestsSGRelationsHashSet =
                _serviceGameRelations.Select(i =>
                {
                    ServiceGame parentGame = _innerIdToServiceGameMap[i.Key];
                    ServiceGame childGame = _innerIdToServiceGameMap[i.Value];
                    return new KeyValuePair<ServiceGame, ServiceGame>(parentGame, childGame);
                })
                .ToHashSet();

            //HashSet связей, которые нужно удалить из таблицы
            HashSet<KeyValuePair<ServiceGame, ServiceGame>> sgRelationsToDeleteHashSet =
                new HashSet<KeyValuePair<ServiceGame, ServiceGame>>(sgRelationsInDBHashSet);
            sgRelationsToDeleteHashSet.ExceptWith(currentRequestsSGRelationsHashSet);
            //HashSet связей, которые нужно добавить в таблицу
            HashSet<KeyValuePair<ServiceGame, ServiceGame>> sgRelationsToAddHashSet =
                new HashSet<KeyValuePair<ServiceGame, ServiceGame>>(currentRequestsSGRelationsHashSet);
            sgRelationsToAddHashSet.ExceptWith(sgRelationsInDBHashSet);

            //удаляем ушедшие связи из таблицы, если запрос был не только по ценам
            if (!_serviceRequestOptions.IsOnlyPrice)
            {
                var sgRelationsToDelete = sgRelationsInDB
                    .Where(i => 
                        sgRelationsToDeleteHashSet.Contains(new KeyValuePair<ServiceGame, ServiceGame>(i.Parent, i.Child))
                    );

                _gameSaver.DeleteSGRelations(sgRelationsToDelete);
            }
            
            //добавляем новые свзяи в таблицу
            var sgRelationsToAdd = sgRelationsToAddHashSet.Select(i => new ServiceGameRelationship
            {
                ParentID = i.Key.ID,
                ChildID = i.Value.ID,
                ServiceCode = _serviceCode
            });
            _gameSaver.InsertSGRelations(sgRelationsToAdd);


            List<RelationChange> relationsChangesList = new List<RelationChange>();
            List<GameRelationship> mgRelationsToAdd = new List<GameRelationship>();
            //получаем список текущих связей в MainGameContext
            List<GameRelationship> mgRelationsInDB = _mainGameContext.GameRelationships
                .AsNoTracking()
                .ToList();
            //превращаем полученный список в HashSet
            HashSet<KeyValuePair<int, int>> mgRelationsInDBHashSet = mgRelationsInDB
                .Select(i => new KeyValuePair<int, int>(i.ParentID, i.ChildID))
                .ToHashSet();
            //перенесим новые связи из ServiceGameContext в GameContext
            foreach (var gameServiceRel in sgRelationsToAddHashSet)
            {
                int parentId = gameServiceRel.Key.MainGameID;
                int childId = gameServiceRel.Value.MainGameID;

                //если такой связи не было в таблице, то пишем ее
                if (!IsMGRelationsHasRelation(parentId, childId))
                {
                    Game pGame = _mainGameContext.Games.Find(parentId);
                    Game cGame = _mainGameContext.Games.Find(childId);
                    //если связь между новыми айтемами, то добавляем ее,
                    //иначе записываем изменения в RelationChanges
                    if (pGame.Status == GameStatus.New && cGame.Status == GameStatus.New)
                    {
                        mgRelationsToAdd.Add(new GameRelationship
                        {
                            ParentID = parentId,
                            ChildID = childId
                        });
                    }
                    else
                    {
                        relationsChangesList.Add(new RelationChange
                        {
                            LeftID = parentId,
                            RightID = childId,
                            ChangeStatus = ChangeStatus.Added
                        });
                    }
                }
            }
            //добавляем новые связи, записанные в лист
            _gameSaver.InsertGRelatios(mgRelationsToAdd);
            //связи, удаленные в ServiceGameContext заносим в список изменений, если запрос не был только по ценам
            if (!_serviceRequestOptions.IsOnlyPrice)
            {
                var mgRelationsToAddChanges = sgRelationsToDeleteHashSet
                    .Where(i => 
                        mgRelationsInDBHashSet.Contains(new KeyValuePair<int, int>(i.Key.MainGameID, i.Value.MainGameID)))
                    .Select(i => new RelationChange 
                    { 
                        LeftID = i.Key.MainGameID, 
                        RightID = i.Value.MainGameID, 
                        ChangeStatus = ChangeStatus.Deleted 
                    });
                
                relationsChangesList.AddRange(mgRelationsToAddChanges);
            }

            return relationsChangesList;

            bool IsMGRelationsHasRelation(int parentId, int childId) =>
                mgRelationsInDBHashSet.Contains(new KeyValuePair<int, int>(parentId, childId));
        }

        private List<Change> UpdateHistory()
        {
            List<Change> updatedChanges = new List<Change>();

            foreach (Change change in _changesList)
            {
                Change existingChange = _historyContext.Changes
                    .FirstOrDefault(i => change.ServiceCode == i.ServiceCode && change.GameID == i.GameID
                        && change.ClassName == i.ClassName && change.ItemID == i.ItemID);

                if (existingChange != null)
                {
                    existingChange.ChangedFields = change.ChangedFields;
                    updatedChanges.Add(existingChange);
                }
                else
                {
                    updatedChanges.Add(change);
                }
            }

            return updatedChanges;
        }

        public void Dispose()
        {
            _imgDownloader?.StopTasksAndDispose();
            _historyContext.Dispose();
            _mainGameContext.Dispose();
            _serviceGameContext.Dispose();
        }
    }
}
