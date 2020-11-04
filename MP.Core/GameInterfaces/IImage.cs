namespace MP.Core.GameInterfaces
{
    public interface IImage
    {
        int ID { get; set; }
        string Name { get; set; }
        int GameID { get; set; }
        public MediaType MediaType { get; set; }
        string Tag { get; set; }
        string Path { get; set; }
    }

    public enum MediaType
    {
        Image,
        Video
    }
}
