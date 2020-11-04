using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using MP.Core.Helpers;
using System.Collections.Generic;
using MP.Core;
using System;
using MP.Scraping.Common;
using MP.Scraping.Common.Configuration;

namespace MP.Scraping.Models.History
{
    public class HistoryContext : DbContext
    {
        public DbSet<Change> Changes { get; set; }
        public DbSet<RelationChange> RelationChanges { get; set; }
        public DbSet<Revision> Revisions { get; set; }

        public HistoryContext() : base()
        {
            Database.SetCommandTimeout((int)TimeSpan.FromMinutes(5).TotalSeconds);
        }

        public HistoryContext(DbContextOptions<HistoryContext> options) : base(options)
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
            modelBuilder.Ignore<Services.Service>();

            modelBuilder.Ignore<Users.User>();

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

            modelBuilder.Entity<RelationChange>()
                .Property(i => i.ChangeStatus)
                .HasConversion(new EnumToStringConverter<ChangeStatus>());

            base.OnModelCreating(modelBuilder);
        }
    }
}
