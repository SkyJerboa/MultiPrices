using System.Collections.Generic;

namespace MP.Core.GameInterfaces
{
    public interface IGame
    {
        int ID { get; set; }
        string Name { get; set; }
        string Languages { get; set; }

        string ImagesPath { get; set; }
        //ICollection<ISystemRequirement> SystemRequirements { get; set; }
        //ICollection<ITranslation> Translations { get; set; }
    }
}
