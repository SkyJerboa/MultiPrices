using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MP.Scraping.GameProcessing.ScrapedGameModels
{
    public class ScrapedGameCollection : IEnumerable<ScrapedGame>
    {
        public List<ScrapedGame> ScrapedGames { get; set; } = new List<ScrapedGame>();

        public ScrapedGame CreateNewScrapedGame()
        {
            ScrapedGame game = new ScrapedGame();
            ScrapedGames.Add(game);
            return game;
        }

        public void RemoveScrapedGame(ScrapedGame game)
        {
            if (ScrapedGames.Contains(game))
                ScrapedGames.Remove(game);
        }

        public IEnumerator<ScrapedGame> GetEnumerator() => ScrapedGames.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
