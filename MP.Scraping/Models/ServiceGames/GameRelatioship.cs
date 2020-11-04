using MP.Scraping.Models.Services;
using MP.Core.GameInterfaces;
using System.ComponentModel.DataAnnotations;

namespace MP.Scraping.Models.ServiceGames
{
    public class ServiceGameRelationship : IGameRelationship
    {
        [Required]
        public string ServiceCode { get; set; }
        public Service Service { get; set; }

        public int ParentID { get; set; }
        public ServiceGame Parent { get; set; }

        public int ChildID { get; set; }
        public ServiceGame Child { get; set; }
    }
}
