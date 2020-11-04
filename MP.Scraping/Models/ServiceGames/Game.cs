using MP.Core.Contexts.Games;
using MP.Core.History;
using MP.Core.Common;
using MP.Scraping.Models.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using MP.Core.GameInterfaces;

namespace MP.Scraping.Models.ServiceGames
{
    public class ServiceGame : IGame, IVersioning<ServiceGame>
    {
        [Key]
        public int ID { get; set; }
        [Required]
        public string ServiceCode { get; set; }
        public Service Service { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        [NotCompare]
        public int MainGameID { get; set; }
        public string InnerID { get; set; }
        public string OfferID { get; set; }
        public GamePlatform Platforms { get; set; }
        public string ImagesPath { get; set; }
        public ServiceGameStatus Status { get; set; }
        [Column(TypeName = "jsonb")]
        public string Languages { get; set; }
        [Column(TypeName = "date")]
        public DateTime? AvailabilityDate { get; set; }
        public string AvailabilityString { get; set; }
        public bool IsAvailable { get; set; }

        [NotCompare]
        [Required]
        public DateTime LastModifyDate { get; set; }

        public ICollection<ServiceGameRelationship> Parents { get; set; }
        public ICollection<ServiceGameRelationship> Children { get; set; }

        public ICollection<SGImage> Images { get; set; }
        public ICollection<SGTranslation> Translations { get; set; }

        public ICollection<SGSystemRequirement> SystemRequirements { get; set; }

        public bool HasChanges(ServiceGame game)
        {
            return !(game.Name == Name && game.OfferID == OfferID
                && game.AvailabilityDate?.Date == AvailabilityDate?.Date && game.AvailabilityString == AvailabilityString 
                && game.IsAvailable == IsAvailable && game.InnerID == InnerID);
        }

        public void ApplyChanges(ServiceGame game)
        {
            Name = game.Name;
            OfferID = game.OfferID;
            InnerID = game.InnerID;
            AvailabilityDate = game.AvailabilityDate;
            AvailabilityString = game.AvailabilityString;
            IsAvailable = game.IsAvailable;
        }

        public void CompareAndChange(ServiceGame game)
        {
            if (HasChanges(game))
                ApplyChanges(game);
        }

        public Dictionary<string,string> GetTransDictionaryByLang(string langCode)
            => Translations.Where(i => i.LanguageCode == langCode).ToDictionary(k => k.Key, v => v.Value);

        public List<SGTranslation> GetTransListByLang(string langCode)
            => Translations.Where(i => i.LanguageCode == langCode).ToList();

        public void AddTranslation(Language lang, string key, string value)
        {
            SGTranslation trans = Translations.FirstOrDefault(i => i.LanguageCode == lang.LangCode && i.Key == key);
            if (trans != null)
            {
                trans.Value = value;
                return;
            }

            trans = new SGTranslation
            {
                Game = this,
                LanguageCode = lang.LangCode,
                Key = key,
                Value = value
            };
            Translations.Add(trans);
        }
    }

    public enum ServiceGameStatus
    {
        NoChange = 0,
        InfoUpdated = 1,
        New = 2,
        Deleted = 3,
        PriceUpdated = 4,
        AvailabilityUpdated = 5
    }
}
