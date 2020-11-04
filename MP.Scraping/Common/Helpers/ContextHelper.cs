using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using MP.Scraping.Models;
using MP.Scraping.Models.ServiceGames;
using System.Linq;

namespace MP.Scraping.Common.Helpers
{
    public static class ContextHelper
    {
        public static void CreateTables<T>(T context) where T : DbContext
        {
            IAnnotation annotation = context.Model.GetAnnotation("Relational:DefaultSchema");
            if (CheckTableExist(context.Database, annotation.Value.ToString()))
                return;


            RelationalDatabaseCreator databaseCreator =
                    context.Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;
            databaseCreator.CreateTables();
        }

        public static bool CheckTableExist(DatabaseFacade db, string schemaName, string tableName = null)
        {
            string cmdStr = "SELECT 1 FROM sys.tables AS T " +
                         "INNER JOIN sys.schemas AS S ON T.schema_id = S.schema_id " +
                         $"WHERE S.Name = '{schemaName}'";
            if (tableName != null)
                cmdStr += $" AND T.Name = '{tableName}'";

            using (var command = db.GetDbConnection().CreateCommand())
            {
                command.CommandText = cmdStr;
                db.OpenConnection();
                using (var result = command.ExecuteReader())
                {
                    return (result.Read());
                }
            }
        }
    }

    public class ServicesModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context)
            => new ServicesModelCacheKey(context);
    }

    class ServicesModelCacheKey : ModelCacheKey
    {
        string _schema;

        public ServicesModelCacheKey(DbContext context)
            : base(context)
        {
            _schema = (context as ServiceGameContext)?.Schema;
        }

        protected override bool Equals(ModelCacheKey other)
            => base.Equals(other)
                && (other as ServicesModelCacheKey)?._schema == _schema;

        public override int GetHashCode()
        {
            var hashCode = base.GetHashCode() * 397;
            if (_schema != null)
            {
                hashCode ^= _schema.GetHashCode();
            }

            return hashCode;
        }
    }
}
