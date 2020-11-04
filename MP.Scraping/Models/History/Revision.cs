using MP.Scraping.Models.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MP.Scraping.Models.History
{
    public class Revision
    {
        [Key]
        public int ID { get; set; }
        [Required]
        public string TableName { get; set; }
        [Required]
        public string ClassName { get; set; }
        public string Key { get; set; }
        [Required]
        public DateTime ChangeDate { get; set; }
        [Column(TypeName = "jsonb")]
        public Dictionary<string, object> OldValue { get; set; }
        [Column(TypeName = "jsonb")]
        public Dictionary<string, object> NewValue { get; set; }
        public string UserName { get; set; }
        public User User { get; set; }
    }
}
