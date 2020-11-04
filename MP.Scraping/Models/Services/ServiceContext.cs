using Microsoft.EntityFrameworkCore;
using MP.Core;
using MP.Core.Helpers;
using MP.Scraping.Common;
using MP.Scraping.Common.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace MP.Scraping.Models.Services
{
    public class ServiceContext : DbContext
    {
        public DbSet<Service> Services { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }
        public DbSet<ServiceCountry> ServiceCountries { get; set; }

        private string _user;


        public ServiceContext() : base()
        { }

        public ServiceContext(DbContextOptions<ServiceContext> options) : base(options)
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
            modelBuilder.Entity<ServiceRequest>()
                .HasOne(i => i.Service)
                .WithMany(i => i.Requests)
                .HasForeignKey(i => i.ServiceCode);

            modelBuilder.Entity<Service>()
                .Ignore(i => i.Games);

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

            modelBuilder.Entity<ServiceRequest>()
                .Ignore(i => i.User);


            base.OnModelCreating(modelBuilder);
        }
    }
}
