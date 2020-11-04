using Microsoft.EntityFrameworkCore;
using MP.Core.Contexts.Games;
using MP.Scraping.Common;
using MP.Scraping.Common.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace MP.Scraping.Models.Games
{
    public class GameWithHistoryContext : GameContext
    {
        private string _user;

        public GameWithHistoryContext(DbContextOptions<GameContext> options) : base(options)
        { }

        public GameWithHistoryContext() : base(ScrapingConfigurationManager.Config.SiteConnection)
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
    }
}
