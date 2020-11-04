using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MP.Client.Models
{
    public class User
    {
        [Key]
        public int ID { get; set; }
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        public bool AllowMailing { get; set; }
        public string SecurityToken { get; set; }
        public int? AccessFailedCount { get; set; }
        public bool EmailConfirmed { get; set; }
        [Required]
        public DateTime CreateDate { get; set; }
    }
}
