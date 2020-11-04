using MP.Core.Contexts.Games;
using MP.Core.Enums;
using MP.Scraping.Common.Helpers;
using MP.Scraping.Models.ServiceGames;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MP.Scraping.GameProcessing.ScrapedGameModels
{
    public class ScrapedGame
    {
        public string InnerID { get; set; }
        public string OfferID { get; set; }
        public bool IsNewGame { get; set; }
        public string Name { get; set; }
        public string NameID { get; set; }
        public string LocalizedName { get; set; }
        public string Description { get; set; }
        public string Developer { get; set; }
        public string Publisher { get; set; }
        public string Brand { get; set; }
        public string LocalizationString { get; private set; }
        public object ReleaseDate { get; set; }
        public GamePlatform? Platforms { get; set; }
        public GameEntityType? GameType { get; set; }
        public SGSystemRequirement[] SystemRequirements { get; set; }
        public string[] ChildrenIDs { get; set; }
        public string[] Tags { get; set; }
        public ScrapedGameImages Images { get; } = new ScrapedGameImages();
        public ScrapedGameOffer Offer { get; } = new ScrapedGameOffer();
        public bool IsAvailable { get; set; }

        public Dictionary<string, object> AdditionalValues = new Dictionary<string, object>();
        
        public void SetLocalizations(SortedDictionary<string, Localization> localizationMap)
        {
            if (localizationMap == null || localizationMap.Count == 0)
                return;

            LocalizationString = JsonConvert.SerializeObject(localizationMap);
        }

        public void ParseAndSetReleaseDate(string dateString, string dateFormat)
        {
            dateString = dateString.ClearDate();

            bool parsed = (dateFormat == null)
                ? DateTime.TryParse(dateString, out DateTime releaseDate)
                : DateTime.TryParseExact(dateString, dateFormat, null, DateTimeStyles.None, out releaseDate);

            if (!parsed)
            {
                parsed = Int32.TryParse(dateString, out int year);
                if (parsed && year >= 1000 && year <= 9999)
                    releaseDate = new DateTime(year, 12, 31);
                else
                    parsed = false;
            }

            ReleaseDate = (parsed) ? releaseDate.Date : (object)dateString;
        }
    }
}
