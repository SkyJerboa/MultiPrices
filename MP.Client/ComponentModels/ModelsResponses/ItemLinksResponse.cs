using MP.Client.Models;

namespace MP.Client.ComponentModels.ModelsResponses
{
    public class ItemLinksResponse
    {
        public string Title { get; set; }
        public string Type { get; set; }
        public ItemLink[] Data { get; set; }
    }
}
