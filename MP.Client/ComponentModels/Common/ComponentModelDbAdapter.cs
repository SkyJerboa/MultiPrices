using System.Collections.Generic;
using System.Data;

namespace MP.Client.ComponentModels.Common
{
    public abstract class ComponentModelDbAdapter<T>
    {
        protected IDbConnection _dbConnection;

        public ComponentModelDbAdapter(IDbConnection connection)
        {
            _dbConnection = connection;
        }

        protected abstract string CreateQuery();

        public abstract IEnumerable<T> ReadData();
    }
}
