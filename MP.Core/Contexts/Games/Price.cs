using System;
using System.ComponentModel.DataAnnotations;

namespace MP.Core.Contexts.Games
{
    public class Price
    {
        [Key]
        public int ID { get; set; }
        public int ServicePriceID { get; set; }
        public PriceInfo PriceInfo { get; set; }
        public float? CurrentPrice { get; set; }
        public float? Discount { get; set; }
        public DateTime ChangingDate { get; set; }

        public bool HasChanges(Price price)
        {
            return price != null && (CurrentPrice != price.CurrentPrice || Discount != price.Discount || ChangingDate != price.ChangingDate);
        }

        public void ApplyChanges(Price price)
        {
            CurrentPrice = price.CurrentPrice;
            Discount = price.Discount;
            ChangingDate = price.ChangingDate;
        }

        public void CompareAndChange(Price price)
        {
            if (HasChanges(price))
                ApplyChanges(price);
        }
    }
}
