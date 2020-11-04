using System.Data;

namespace MP.Client.ComponentModels.Common
{
    public class ComponentModelOptions
    {
        public IDbConnection Connection { get; }
        public string Title { get; }
        public string Country { get; }
        public string Currency { get; }

        public ComponentModelOptions(
            IDbConnection connection, 
            string title, 
            string country, 
            string currency)
        {
            Connection = connection;
            Title = title;
            Country = country;
            Currency = currency;
        }
    }
}
