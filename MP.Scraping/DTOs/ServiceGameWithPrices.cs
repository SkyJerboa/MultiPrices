using MP.Core.Contexts.Games;
using MP.Scraping.Models.ServiceGames;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MP.Scraping.DTOs
{
    public class ServiceGameWithPrices : ServiceGame
    {
        public ICollection<PriceInfo> CountryPrices { get; set; }

        public ServiceGameWithPrices()
        { }

        public ServiceGameWithPrices(ServiceGame game)
        {
            if (game == null)
                return;

            Type type = typeof(ServiceGameWithPrices);

            foreach (PropertyInfo member in game.GetType().GetProperties())
                type.GetProperty(member.Name).SetValue(this, member.GetValue(game));

        }

        public ServiceGame CreateServiceGameInstance()
        {
            ServiceGame sg = new ServiceGame();

            Type typeSg = typeof(ServiceGame);
            Type typeSgp = typeof(ServiceGameWithPrices);

            foreach (PropertyInfo member in typeSg.GetProperties())
                member.SetValue(sg, typeSgp.GetProperty(member.Name).GetValue(this));

            return sg;
        }
    }
}
