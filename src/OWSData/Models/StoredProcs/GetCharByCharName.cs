using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace OWSData.Models.StoredProcs
{
    public class GetCharByCharName
    {
        public GetCharByCharName()
        {
            IsAdmin = false;
            IsModerator = false;
        }

        public Guid CustomerGuid { get; set; }
        public int CharacterId { get; set; }
        public Guid? UserGuid { get; set; }
        public string Email { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsModerator { get; set; }

        [JsonPropertyName("CharacterName")]
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
        public int Port { get; set; }
        public int MapInstanceID { get; set; }
        public string ClassName { get; set; }
    }
}
