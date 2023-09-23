using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    public partial class Characters
    {
        public Characters()
        {
            CharHasAbilities = new HashSet<CharHasAbilities>();
            CharHasItems = new HashSet<CharHasItems>();
            CustomCharacterData = new HashSet<CustomCharacterData>();
        }

        public Guid CustomerGuid { get; set; }
        public int CharacterId { get; set; }
        public Guid? UserGuid { get; set; }
        public string Email { get; set; }
        public string CharName { get; set; }
        public string MapName { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public string ServerIp { get; set; }
        public DateTime LastActivity { get; set; }
        public double Rx { get; set; }
        public double Ry { get; set; }
        public double Rz { get; set; }
        public int TeamNumber { get; set; }
        public short CharacterLevel { get; set; }
        public byte Gender { get; set; }
        public string Description { get; set; }
        public string DefaultPawnClassPath { get; set; }
        public bool IsInternalNetworkTestUser { get; set; }
        public int ClassId { get; set; }
        public string BaseMesh { get; set; }

        public ICollection<CharHasAbilities> CharHasAbilities { get; set; }
        public ICollection<CharHasItems> CharHasItems { get; set; }
        public ICollection<CustomCharacterData> CustomCharacterData { get; set; }
    }
}
