using MP.Core.Contexts.Games;
using MP.Core.GameInterfaces;
using System;
using System.Reflection;

namespace MP.Client.SiteModels.GameModels.GameFullInfo
{
    public class SystemRequirement
    {
        public RequirementType Type { get; set; }
        public OSType SystemType { get; set; }
        public string OS { get; set; }
        public string CPU { get; set; }
        public string GPU { get; set; }
        public string RAM { get; set; }
        public string Storage { get; set; }
        public string DirectX { get; set; }
        public string Sound { get; set; }
        public string Network { get; set; }
        public string Other { get; set; }
    }
}
