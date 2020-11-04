using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MP.Core.Common;
using MP.Core.Common.Configuration;
using MP.Core.GameInterfaces;

namespace MP.Core.Contexts.Games
{
    public class GameContext: DbContext
    {
        public DbSet<Game> Games { get; set; }
        public DbSet<GameRelationship> GameRelationships { get; set; }
        public DbSet<PriceInfo> PriceInfos { get; set; }
        public DbSet<Price> Prices { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<GImage> Images { get; set; }
        public DbSet<GSystemRequirement> SystemRequirements { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<GameTagRelation> GameTagRelations { get; set; }

        public DbSet<Language> Languages { get; set; }
        public DbSet<GTranslation> GameTranslations { get; set; }

        string _connectionString;

        public GameContext(DbContextOptions<GameContext> options) : base(options)
        { }

        //public GameContext() : base()
        //{
        //    _connectionString = SimpleConfigurationManager.DefaultConnection;
        //    //Database.SetCommandTimeout((int)TimeSpan.FromMinutes(5).TotalSeconds);
        //}
        
        public GameContext(string connectionString) : base()
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(_connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Game>()
                  .Property(i => i.GamePlatform)
                  .HasConversion(new EnumToStringConverter<GamePlatform>());

            modelBuilder.Entity<Game>()
                .Property(i => i.Status)
                .HasConversion(new EnumToStringConverter<GameStatus>());

            modelBuilder.Entity<Game>()
                .Property(i => i.GameType)
                .HasConversion(new EnumToStringConverter<GameEntityType>());

            modelBuilder.Entity<Game>()
                .HasIndex(i => i.NameID)
                .IsUnique();

            modelBuilder.Entity<PriceInfo>()
                .HasOne(i => i.Game)
                .WithMany(i => i.PriceInfos)
                .HasForeignKey(i => i.GameID);

            modelBuilder.Entity<PriceInfo>()
                .HasOne(i => i.Currency)
                .WithMany()
                .HasForeignKey(i => i.CurrencyCode)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PriceInfo>()
                .HasOne(i => i.Service)
                .WithMany()
                .HasForeignKey(i => i.ServiceCode)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Price>()
                .HasOne(i => i.PriceInfo)
                .WithMany(i => i.Prices)
                .HasForeignKey(i => i.ServicePriceID);

            modelBuilder.Entity<GameRelationship>()
                .HasKey(k => new { k.ChildID, k.ParentID });

            modelBuilder.Entity<GameRelationship>()
                .HasOne(i => i.Parent)
                .WithMany(i => i.Children)
                .HasForeignKey(i => i.ParentID);

            modelBuilder.Entity<GameRelationship>()
                .HasOne(i => i.Child)
                .WithMany(i => i.Parents)
                .HasForeignKey(i => i.ChildID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GTranslation>()
                .HasOne(i => i.Game)
                .WithMany(i => i.Translations)
                .HasForeignKey(i => i.GameID);

            modelBuilder.Entity<GTranslation>()
                .HasOne(i => i.Language)
                .WithMany()
                .HasForeignKey(i => i.LanguageCode)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GTranslation>()
                .HasKey(i => new { i.GameID, i.LanguageCode, i.Key });

            modelBuilder.Entity<GImage>()
                .Property(i => i.MediaType)
                .HasConversion(new EnumToStringConverter<MediaType>());

            modelBuilder.Entity<GImage>()
                .HasOne(i => i.Game)
                .WithMany(i => i.Images)
                .HasForeignKey(i => i.GameID);

            modelBuilder.Entity<GSystemRequirement>()
                .Property(i => i.Type)
                .HasConversion(new EnumToStringConverter<RequirementType>());

            modelBuilder.Entity<GSystemRequirement>()
                .Property(i => i.SystemType)
                .HasConversion(new EnumToStringConverter<OSType>());

            modelBuilder.Entity<GSystemRequirement>()
                .HasOne(i => i.Game)
                .WithMany(i => i.SystemRequirements)
                .HasForeignKey(i => i.GameID);

            modelBuilder.Entity<GSystemRequirement>()
                .HasIndex(i => new { i.GameID, i.SystemType, i.Type})
                .IsUnique();

            modelBuilder.Entity<Tag>()
                .HasIndex(i => i.Name)
                .IsUnique();

            modelBuilder.Entity<GameTagRelation>()
                .HasKey(i => new { i.GameID, i.TagID });

            modelBuilder.Entity<GameTagRelation>()
                .HasOne(i => i.Game)
                .WithMany(i => i.Tags)
                .HasForeignKey(i => i.GameID);

            modelBuilder.Entity<GameTagRelation>()
                .HasOne(i => i.Tag)
                .WithMany(i => i.Games)
                .HasForeignKey(i => i.TagID);

            base.OnModelCreating(modelBuilder);
        }
    }
}
