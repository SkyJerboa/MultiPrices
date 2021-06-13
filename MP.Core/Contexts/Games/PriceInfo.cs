using MP.Core.History;
using MP.Core.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System;

namespace MP.Core.Contexts.Games
{
    public class PriceInfo
    {
        [Key]
        public int ID { get; set; }
        [MaxLength(5)]
        [Required]
        public string ServiceCode { get; set; }
        public GService Service { get; set; }
        public int GameID { get; set; }
        public Game Game { get; set; }
        [MaxLength(5)]
        [Required]
        public string CountryCode { get; set; }
        [Required]
        public string CurrencyCode { get; set; }
        public Currency Currency { get; set; }
        public string GameLink { get; set; }
        public float? CurrentPrice { get; set; }
        public float? FullPrice { get; set; }
        public float? Discount { get; set; }
        public DateTime? DiscountEndDate { get; set; }
        public bool IsFree { get; set; }
        public bool IsAvailable { get; set; } = true;
        public bool IsPreorder { get; set; }
        public bool IsIgnore { get; set; }
        public bool IsPersistent { get; set; }
        public ICollection<Price> Prices { get; set; } = new List<Price>();

        public bool HasChanges(PriceInfo priceInfo)
        {
            return CurrencyCode != priceInfo.CurrencyCode || GameLink != priceInfo.GameLink
                || CurrentPrice != priceInfo.CurrentPrice || FullPrice != priceInfo.FullPrice
                || Discount != priceInfo.Discount || IsFree != priceInfo.IsFree
                || IsPreorder != priceInfo.IsPreorder || IsAvailable != priceInfo.IsAvailable
                || IsIgnore != priceInfo.IsIgnore || IsPersistent != priceInfo.IsPersistent 
                || DiscountEndDate != priceInfo.DiscountEndDate;
        }

        public void ApplyChanges(PriceInfo priceInfo)
        {
            CurrencyCode = priceInfo.CurrencyCode;
            GameLink = priceInfo.GameLink;
            CurrentPrice = priceInfo.CurrentPrice;
            FullPrice = priceInfo.FullPrice;
            Discount = priceInfo.Discount;
            IsFree = priceInfo.IsFree;
            IsPreorder = priceInfo.IsPreorder;
            IsAvailable = priceInfo.IsAvailable;
            IsIgnore = priceInfo.IsIgnore;
            IsPersistent = priceInfo.IsPersistent;
            DiscountEndDate = priceInfo.DiscountEndDate;
        }

        public void CompareAndChange(PriceInfo priceInfo)
        {
            if (!HasChanges(priceInfo))
                return;

            ApplyChanges(priceInfo);
        }

        public Price GetLastPrice() => Prices.OrderByDescending(i => i.ChangingDate).FirstOrDefault();

        public void UpdateOnlyPrice(PriceInfo priceInfo)
        {
            CurrentPrice = priceInfo.CurrentPrice;
            FullPrice = priceInfo.FullPrice;
            Discount = priceInfo.Discount;
            IsFree = (FullPrice == 0);
        }

        public bool HasPriceChanges(PriceInfo priceInfo)
        {
            if (CurrencyCode != priceInfo.CurrencyCode || CountryCode != priceInfo.CountryCode)
                throw new InvalidOperationException();

            return CurrentPrice != priceInfo.CurrentPrice || FullPrice != priceInfo.FullPrice || Discount != priceInfo.Discount;
        }
    }
}
