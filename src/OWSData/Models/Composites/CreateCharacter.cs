using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSData.Models.Composites
{
    public class CreateCharacter
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

        public string CharacterName { get; set; }
        public string ClassName { get; set; }
        public int CharacterLevel { get; set; }
        public string StartingMapName { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float RX { get; set; }
        public float RY { get; set; }
        public float RZ { get; set; }
        public int TeamNumber { get; set; }
        public int Gender { get; set; }
    }
}
