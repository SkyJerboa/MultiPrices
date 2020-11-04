using System.ComponentModel.DataAnnotations;

namespace MP.Scraping.Models.History
{
    public class RelationChange
    {
        [Key]
        public int ID { get; set; }
        [Required]
        public int LeftID { get; set; }
        [Required]
        public int RightID { get; set; }
        public ChangeStatus ChangeStatus { get; set; }
    }

    public enum ChangeStatus
    {
        Added = 1,
        Updated = 0,
        Deleted = 2
    }
}
