using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MP.Scraping.GameProcessing.ScrapedGameModels
{
    public class ScrapedGameImages
    {
        public string Horizontal { get; set; }
        public string Vertical { get; set; }
        public string LongHeader { get; set; }
        public string LogoPng { get; set; }
        public string[]  Screenshots { get; set; }
    }
}
