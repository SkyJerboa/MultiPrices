using MP.Client.ComponentModels.Common;
using MP.Client.ComponentModels.ModelsResponses;
using MP.Client.Models;

namespace MP.Client.ComponentModels
{
    public class ItemLinks : IComponentModel
    {
        public ItemLink[] Items { get; set; }

        public object CreateResponseObject(ComponentModelOptions options)
        {
            if (Items.Length == 0)
                return null;

            var responseObject = new ItemLinksResponse
            {
                Title = options.Title,
                Type = nameof(ItemLinks),
                Data = Items
            };

            return responseObject;
        }
    }

    public class ItemLink
    {
        public string Title { get; set; }
        public string Image { get; set; }
        public string Link { get; set; }
    }
}
