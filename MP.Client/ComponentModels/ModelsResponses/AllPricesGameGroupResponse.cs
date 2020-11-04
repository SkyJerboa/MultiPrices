using MP.Client.SiteModels.GameModels;
using System.Collections.Generic;
using System.Linq;

namespace MP.Client.ComponentModels.ModelsResponses
{
    public class AllPricesGameGroupResponse
    {
        public string Title { get; set; }
        public string Type { get; set; }
        public IEnumerable<GroupModel> Data { get; set; }

        public class GroupModel
        {
            public string GroupName { get; set; }
            public IGrouping<string, GroupedWithServiceGame> Data { get; set; }
        }
    }

    
}
