using System.ComponentModel.DataAnnotations;

namespace MP.Scraping.Models.Users
{
    public class User
    {
        [Key]
        public string UserName { get; set; }
        public UserRole Role { get; set; }
        public string Title { get; set; }
        [Required]
        public string Password { get; set; }
        public bool IsEnable { get; set; }
    }

    public enum UserRole
    {
        Viewer,
        Editor,
        Scraper,
        Admin
    }
}
