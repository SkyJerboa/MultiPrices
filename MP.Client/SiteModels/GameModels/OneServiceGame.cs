namespace MP.Client.SiteModels.GameModels
{
    //Game with single service
    public class OneServiceGame
    {
        public string Name { get; set; }
        public string NameID { get; set; }
        public float? FullPrice { get; set; }
        public float? CurrentPrice { get; set; }
        public float? Discount { get; set; }
        public string ServiceCode { get; set; }
        public string ImageVertical { get; set; }
        public string ImageHorizontal { get; set; }
    }
}
