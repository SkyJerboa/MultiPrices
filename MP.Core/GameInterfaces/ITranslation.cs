namespace MP.Core.GameInterfaces
{
    public interface ITranslation
    {
        string LanguageCode { get; set; }
        int GameID { get; set; }
        string Key { get; set; }
        string Value { get; set; }
    }
}
