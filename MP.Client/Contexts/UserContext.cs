using Microsoft.EntityFrameworkCore;
using MP.Client.Common;
using MP.Client.Common.Configuration;
using MP.Client.Models;
using MP.Core;

namespace MP.Client.Contexts
{
    public class UserContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        public UserContext(DbContextOptions<UserContext> options) : base(options)
        { }

        public UserContext() : base()
        { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(SiteConfigurationManager.DefaultConnection);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(i => i.UserName)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(i => i.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(i => i.AllowMailing)
                .HasDefaultValue(false);

            modelBuilder.Entity<User>()
                .Property(i => i.EmailConfirmed)
                .HasDefaultValue(false);

            base.OnModelCreating(modelBuilder);
        }
    }
}
