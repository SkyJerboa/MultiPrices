using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace MP.Core.Contexts.Translations
{
    public class TranslationContext : DbContext
    {
        public DbSet<Language> Languages { get; set; }
        public DbSet<Translation> Translations { get; set; }

        public TranslationContext(DbContextOptions<TranslationContext> options) : base(options)
        {
        }

        public TranslationContext() : base()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(Settings.DefaultConnection);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Translation>()
                .HasOne(i => i.Language)
                .WithMany(i => i.Translations)
                .HasForeignKey(i => i.LanguageCode);

            modelBuilder.Entity<Translation>()
                .HasIndex(i => new { i.LanguageCode, i.Key })
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}
