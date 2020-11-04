using System.ComponentModel.DataAnnotations;

namespace MP.Core.Contexts.History
{
    public class RelationChange
    {
        public int ID { get; set; }
        [Required]
        public int LeftID { get; set; }
        [Required]
        public int RightID { get; set; }
        [Required]
        public ChangeStatus ChangeStatus { get; set; }
    }

    public enum ChangeStatus
    {
        Added = 1,
        Updated = 0,
        Deleted = 2
    }
}
