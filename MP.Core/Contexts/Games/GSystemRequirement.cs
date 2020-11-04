using MP.Core.Common;
using MP.Core.History;
using System;

namespace MP.Core.Contexts.Games
{
    public class GSystemRequirement : SystemRequirement, ICloneable, IVersioning
    {
        public Game Game { get; set; }

        public object Clone()
        {
            return new GSystemRequirement
            {
                Type = Type,
                SystemType = SystemType,
                GameID = GameID,
                OS = OS,
                CPU = CPU,
                DirectX = DirectX,
                GPU = GPU,
                Network = Network,
                Other = Other,
                RAM = RAM,
                Sound = Sound,
                Storage = Storage
            };
        }
    }

    
}
