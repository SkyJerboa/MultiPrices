using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MP.Core;
using MP.Core.Common;
using MP.Core.GameInterfaces;
using MP.Core.Helpers;
using MP.Scraping.Common;
using MP.Scraping.Common.Configuration;
using MP.Scraping.Models.History;
using MP.Scraping.Models.ServiceGames;
using MP.Scraping.Models.Services;
using MP.Scraping.Models.Users;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace MP.Scraping.Models
{
    public class MainContext : DbContext
    {
        public DbSet<Service> Services { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }
        public DbSet<ServiceCountry> ServiceCountries { get; set; }

        public DbSet<ServiceGame> Games { get; set; }
        public DbSet<ServiceGameRelationship> GameRelationships { get; set; }
        public DbSet<SGImage> Images { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<SGTranslation> Translations { get; set; }
        public DbSet<SGSystemRequirement> SystemRequirements { get; set; }
        public DbSet<TagMap> TagsMap { get; set; }

        public DbSet<Change> Changes { get; set; }
        public DbSet<RelationChange> RelationChanges { get; set; }
        public DbSet<Revision> Revisions { get; set; }

        public DbSet<User> Users { get; set; }

        public MainContext(DbContextOptions<MainContext> options) : base(options)
        { }

        public MainContext() : base()
        { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(ScrapingConfigurationManager.DefaultConnection);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ServiceRequest>()
                .HasOne(i => i.Service)
                .WithMany(i => i.Requests)
                .HasForeignKey(i => i.ServiceCode)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ServiceRequest>()
                .HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(i => i.UserName)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ServiceCountry>()
                .Property(i => i.CurrencyList)
                .HasConversion(new ListToJsonStringConverter());

            modelBuilder.Entity<ServiceCountry>()
                .Property(i => i.LanguageList)
                .HasConversion(new ListToJsonStringConverter());

            modelBuilder.Entity<ServiceCountry>()
                .HasOne(i => i.Service)
                .WithMany(i => i.SupportedCountries)
                .HasForeignKey(i => i.ServiceCode);

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
                .HasForeignKey(i => i.ServiceCode)
                .OnDelete(DeleteBehavior.Restrict);

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
                .HasForeignKey(i => i.ServiceCode)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SGTranslation>()
                .HasOne(i => i.Game)
                .WithMany(i => i.Translations)
                .HasForeignKey(i => i.GameID);

            modelBuilder.Entity<SGTranslation>()
                .HasOne(i => i.Language)
                .WithMany()
                .HasForeignKey(i => i.LanguageCode)
                .OnDelete(DeleteBehavior.Restrict);

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

            modelBuilder.Entity<SGImage>()
                .Property(i => i.MediaType)
                .HasConversion(new EnumToStringConverter<MediaType>());

            modelBuilder.Entity<SGImage>()
                .HasOne(i => i.Game)
                .WithMany(i => i.Images)
                .HasForeignKey(i => i.GameID);

            modelBuilder.Entity<Change>()
                .HasOne(i => i.Service)
                .WithMany()
                .HasForeignKey(i => i.ServiceCode)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Change>()
                .Property(i => i.ChangedFields)
                .HasConversion(
                    v => JsonConvert.SerializeObject(v, new JsonSerializerSettings
                    {
                        Converters = new List<JsonConverter>() { new Newtonsoft.Json.Converters.StringEnumConverter() }
                    }),
                    v => JsonConvert.DeserializeObject<Dictionary<string, object>>(v));

            modelBuilder.Entity<Revision>()
                .Property(i => i.OldValue)
                .HasConversion(new DictionaryToStringConverter());

            modelBuilder.Entity<Revision>()
                .Property(i => i.NewValue)
                .HasConversion(new DictionaryToStringConverter());

            modelBuilder.Entity<Revision>()
                .HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(i => i.UserName)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RelationChange>()
                .Property(i => i.ChangeStatus)
                .HasConversion(new EnumToStringConverter<ChangeStatus>());

            modelBuilder.Entity<User>()
                .Property(i => i.Role)
                .HasConversion(new EnumToStringConverter<UserRole>());

            
            base.OnModelCreating(modelBuilder);
        }
    }
}
