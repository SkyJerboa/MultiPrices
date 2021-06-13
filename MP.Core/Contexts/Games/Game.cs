using MP.Core.Enums;
using MP.Core.GameInterfaces;
using MP.Core.History;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MP.Core.Contexts.Games
{
    public class Game : IGame, IVersioning<Game>
    {
        [Key]
        public int ID { get; set; }
        [Required]
        [NotCompare]
        public string Name { get; set; }
        [Required]
        public string NameID { get; set; }
        [Required]
        [NotCompare]
        public GameStatus Status { get; set; }
        public GameEntityType GameType { get; set; }
        public GamePlatform GamePlatform { get; set; }
        [NotCompare]
        [Column(TypeName = "jsonb")]
        public string[] GameServicesCodes { get; set; }
        public string Publisher { get; set; }
        public string Developer { get; set; }
        public string Brand { get; set; }
        [NotCompare]
        public string ImagesPath { get; set; }
        [NotCompare]
        [Column(TypeName = "jsonb")]
        public string Languages { get; set; }
        [Column(TypeName = "date")]
        public DateTime? ReleaseDate { get; set; }
        [Required]
        [NotCompare]
        public int Order { get; set; }
        [Required]
        [NotCompare]
        public DateTime LastModifyDate { get; set; }

        public ICollection<GameRelationship> Parents { get; set; }
        public ICollection<GameRelationship> Children { get; set; }
        public ICollection<PriceInfo> PriceInfos { get; set; } = new List<PriceInfo>();
        public ICollection<GImage> Images { get; set; } = new List<GImage>();
        public ICollection<GSystemRequirement> SystemRequirements { get; set; } = new List<GSystemRequirement>();
        public ICollection<GTranslation> Translations { get; set; } = new List<GTranslation>();
        public ICollection<GameTagRelation> Tags { get; set; } = new List<GameTagRelation>();

        public bool HasChanges(Game game)
        {
            return !(Name == game.Name && (GameType == game.GameType || game.GameType == GameEntityType.Unknown)
                && game.GamePlatform <= GamePlatform && (string.IsNullOrEmpty(game.Publisher)
                || Publisher == game.Publisher) && (Developer == game.Developer || string.IsNullOrEmpty(game.Developer))
                && Order == game.Order);
        }

        public void ApplyChanges(Game game)
        {
            Name = game.Name;
            GameType = game.GameType;
            GamePlatform |= game.GamePlatform;
            Publisher = game.Publisher;
            Developer = game.Developer;
            Order = game.Order;
        }

        public void CompareAndChange(Game game)
        {
            if (HasChanges(game))
                ApplyChanges(game);
        }

        public void AddServiceCode(string code)
        {
            string[] tempArr = new string[GameServicesCodes.Length + 1];
            GameServicesCodes.CopyTo(tempArr, 0);
            tempArr[tempArr.Length - 1] = code;
            GameServicesCodes = tempArr;
        }

        public void RemoveServiceCode(string code) => GameServicesCodes = GameServicesCodes.Where(i => i != code).ToArray();

        public void MergeLocalizations(string text)
        {
            if (String.IsNullOrEmpty(text))
                return;

            if (Languages == null)
            {
                Languages = text;
                return;
            }
            
            Dictionary<string, Localization> newLocs = JsonConvert.DeserializeObject<Dictionary<string, Localization>>(text);

            if (newLocs == null || newLocs.Count == 0)
                return;

            Dictionary<string, Localization> existingLocs =
                       JsonConvert.DeserializeObject<Dictionary<string, Localization>>(Languages);

            foreach (var loc in newLocs)
            {
                if (!existingLocs.ContainsKey(loc.Key))
                    existingLocs.Add(loc.Key, loc.Value);
                else if (existingLocs[loc.Key] < loc.Value)
                    existingLocs[loc.Key] = loc.Value;
            }

            Languages = JsonConvert.SerializeObject(existingLocs);
        }
    }

    public enum GameEntityType
    {
        Unknown = 0,
        FullGame = 1,
        DLC = 2,
        Edition = 3,
        Pack = 4,
        Demo = 5,
        Software = 6
    }

    [Flags]
    public enum GamePlatform
    {
        Unknown = 0,
        Windows = 1,
        Mac = 2,
        Linux = 4,
        Android = 8,
        IOS = 16,
        PC = Windows | Mac | Linux,
        Mobile = Android | IOS
    }

    public enum GameStatus
    {
        New = 0,
        Completed = 1,
        Deleted = 2,
        NotAvailable = 3,
        Returned = 4
    }
}
