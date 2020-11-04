using MP.Core.GameInterfaces;
using MP.Core.History;
using System;
using System.ComponentModel.DataAnnotations;

namespace MP.Core.Common
{
    public class SystemRequirement : ISystemRequirement
    {
        [Key]
        public int ID { get; set; }
        [NotCompare]
        public int GameID { get; set; }
        public RequirementType Type { get; set; }
        public OSType SystemType { get; set; }
        public string OS { get; set; }
        public string CPU { get; set; }
        public string GPU { get; set; }
        [MaxLength(10)]
        public string RAM { get; set; }
        [MaxLength(10)]
        public string Storage { get; set; }
        [MaxLength(20)]
        public string DirectX { get; set; }
        public string Sound { get; set; }
        public string Network { get; set; }
        public string Other { get; set; }

        public void CompareAndChange(SystemRequirement sysReq)
        {
            if (HasChanges(sysReq))
                ApplyChanges(sysReq);
        }

        private bool HasChanges(SystemRequirement sysReq)
        {
            if (sysReq.Type != Type || sysReq.SystemType != SystemType)
                return false;

            return (sysReq.OS != OS || sysReq.CPU != CPU || sysReq.GPU != GPU || sysReq.RAM != RAM || sysReq.Storage != Storage
                || sysReq.DirectX != DirectX || sysReq.Sound != Sound || sysReq.Network != Network || sysReq.Other != Other);
        }

        private void ApplyChanges(SystemRequirement sysReq)
        {
            OS = sysReq.OS;
            CPU = sysReq.CPU;
            GPU = sysReq.GPU;
            RAM = sysReq.RAM;
            Storage = sysReq.Storage;
            DirectX = sysReq.DirectX;
            Sound = sysReq.Sound;
            Network = sysReq.Network;
            Other = sysReq.Other;
        }

        public T CreateCopy<T>() where T : SystemRequirement, new()
        {
            T copiedSysReq = new T()
            {
                OS = OS,
                CPU = CPU,
                GPU = GPU,
                RAM = RAM,
                Storage = Storage,
                DirectX = DirectX,
                Sound = Sound,
                Network = Network,
                Other = Other,
                SystemType = SystemType,
                Type = Type
            };

            return copiedSysReq;
        }
    }
}
