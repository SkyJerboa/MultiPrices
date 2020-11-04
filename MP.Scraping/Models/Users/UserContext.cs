using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MP.Core;
using MP.Scraping.Common;
using MP.Scraping.Common.Configuration;

namespace MP.Scraping.Models.Users
{
    public class UserContext: DbContext
    {
        public DbSet<User> Users { get; set; }

        public UserContext (DbContextOptions<UserContext> options) : base(options)
        { }

        public UserContext() : base()
        { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(ScrapingConfigurationManager.DefaultConnection);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .Property(i => i.Role)
                .HasConversion(new EnumToStringConverter<UserRole>());
        }
    }
}
