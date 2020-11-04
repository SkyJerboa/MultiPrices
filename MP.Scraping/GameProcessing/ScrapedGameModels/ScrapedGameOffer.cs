using System;

namespace MP.Scraping.GameProcessing.ScrapedGameModels
{
    public class ScrapedGameOffer
    {
        public string URL { get; set; }
        public float? FullPrice { get; set; }
        public float? CurrentPrice { get; set; }
        public float? Discount { get; set; }

        public bool IsPreorder { get; set; }

        public void CalculateAndSetDiscount()
        {
            if (FullPrice == null || CurrentPrice == null)
                return;

            float discount = ((float)FullPrice == 0) 
                ? 0 
                : 100 * ((float)FullPrice - (float)CurrentPrice) / (float)FullPrice;
            Discount = (float)Math.Round(discount);
        }
    }
}
