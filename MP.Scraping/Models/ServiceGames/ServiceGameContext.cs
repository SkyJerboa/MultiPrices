using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MP.Core;
using MP.Core.GameInterfaces;
using MP.Scraping.Common;
using MP.Scraping.Common.Configuration;

namespace MP.Scraping.Models.ServiceGames
{
    public class ServiceGameContext : DbContext
    {
        public DbSet<ServiceGame> Games { get; set; }
        public DbSet<ServiceGameRelationship> GameRelationships { get; set; }
        public DbSet<SGTranslation> Translations { get; set; }
        public DbSet<SGImage> Images { get; set; }
        public DbSet<TagMap> TagsMap { get; set; }
        public DbSet<SGSystemRequirement> SystemRequirements { get; set; }

        private string _user;

        public ServiceGameContext()
        {
            Database.SetCommandTimeout((int)TimeSpan.FromMinutes(5).TotalSeconds);
        }

        public ServiceGameContext(DbContextOptions<ServiceGameContext> options) : base(options)
        { }

        public int SaveChanges(string user)
        {
            _user = user;
            return base.SaveChanges();
        }

        public Task<int> SaveChangesAsync(string user)
        {
            _user = user;
            return base.SaveChangesAsync();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            RevisionCreator.CreateRevisions(ChangeTracker, user: _user);
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            RevisionCreator.CreateRevisions(ChangeTracker, user: _user);
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(ScrapingConfigurationManager.DefaultConnection);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ServiceGame>()
                .Property(i => i.Platforms)
                .HasConversion(new EnumToStringConverter<Core.Contexts.Games.GamePlatform>());

            modelBuilder.Entity<ServiceGame>()
                .Property(i => i.Status)
                .HasConversion(new EnumToStringConverter<ServiceGameStatus>());

            modelBuilder.Entity<ServiceGame>()
                .HasIndex(i => new { i.ServiceCode, i.MainGameID })
                .IsUnique();

            modelBuilder.Entity<ServiceGame>()
                .HasOne(i => i.Service)
                .WithMany(i => i.Games)
                .HasForeignKey(i => i.ServiceCode);

            modelBuilder.Entity<ServiceGameRelationship>()
                .HasKey(k => new { k.ChildID, k.ParentID });

            modelBuilder.Entity<ServiceGameRelationship>()
                .HasOne(i => i.Parent)
                .WithMany(i => i.Children)
                .HasForeignKey(i => i.ParentID);

            modelBuilder.Entity<ServiceGameRelationship>()
                .HasOne(i => i.Child)
                .WithMany(i => i.Parents)
                .HasForeignKey(i => i.ChildID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ServiceGameRelationship>()
                .HasOne(i => i.Service)
                .WithMany()
                .HasForeignKey(i => i.ServiceCode);

            modelBuilder.Entity<SGTranslation>()
                .HasOne(i => i.Game)
                .WithMany(i => i.Translations)
                .HasForeignKey(i => i.GameID);

            modelBuilder.Entity<SGTranslation>()
                .HasIndex(i => new { i.LanguageCode, i.GameID, i.Key })
                .IsUnique();

            modelBuilder.Entity<SGTranslation>()
                .Ignore(i => i.Language);

            modelBuilder.Entity<SGImage>()
                .Property(i => i.MediaType)
                .HasConversion(new EnumToStringConverter<MediaType>());

            modelBuilder.Entity<SGImage>()
                .HasOne(i => i.Game)
                .WithMany(i => i.Images)
                .HasForeignKey(i => i.GameID);

            modelBuilder.Entity<SGSystemRequirement>()
                .Property(i => i.Type)
                .HasConversion(new EnumToStringConverter<RequirementType>());

            modelBuilder.Entity<SGSystemRequirement>()
                .Property(i => i.SystemType)
                .HasConversion(new EnumToStringConverter<OSType>());

            modelBuilder.Entity<SGSystemRequirement>()
                .HasOne(i => i.Game)
                .WithMany(i => i.SystemRequirements)
                .HasForeignKey(i => i.GameID);

            modelBuilder.Entity<SGSystemRequirement>()
                .HasIndex(i => new { i.GameID, i.SystemType, i.Type })
                .IsUnique();

            modelBuilder.Entity<Services.Service>()
                .Ignore(i => i.SupportedCountries);

            base.OnModelCreating(modelBuilder);
        }

        public IQueryable<ServiceGame> GetServiceGamesWithoutTracking(string serviceCode)
        {
            return Games
                .AsNoTracking()
                .Include(i => i.Children)
                    .ThenInclude(i => i.Child)
                .Include(i => i.Images)
                .Include(i => i.Translations)
                .Include(i => i.SystemRequirements)
                .Where(i => i.ServiceCode == serviceCode.ToUpper());
        }
    }
}
