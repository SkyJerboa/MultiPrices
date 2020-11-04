namespace MP.Core.GameInterfaces
{
    public interface ISystemRequirement
    {
        int ID { get; set; }
        RequirementType Type { get; set; }
        OSType SystemType { get; set; }
        int GameID { get; set; }
        string OS { get; set; }
        string CPU { get; set; }
        string GPU { get; set; }
        string RAM { get; set; }
        string Storage { get; set; }
        string DirectX { get; set; }
        string Sound { get; set; }
        string Network { get; set; }
        string Other { get; set; }
    }

    public enum RequirementType
    {
        Minimum,
        Recommended,
        Maximum
    }

    public enum OSType
    {
        Unknown,
        OSX,
        Linux,
        Windows
    }
}
