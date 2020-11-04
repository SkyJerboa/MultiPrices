using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using MP.Core.Helpers;
using System.Collections.Generic;

namespace MP.Core.Contexts.History
{
    public class HistoryContext : DbContext
    {
        public DbSet<Change> Changes { get; set; }
        public DbSet<RelationChange> RelationChanges { get; set; }
        public DbSet<Revision> Revisions { get; set; }

        public HistoryContext() : base()
        { }

        public HistoryContext(DbContextOptions<HistoryContext> options) : base(options)
        { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(Settings.DefaultConnection);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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
