using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using OWSData.Models.Tables;

namespace OWSData.Models.Composites
{
    // Inherit from Characters
    public class CharactersExtended: Characters
    {
        // Permissions
        public bool IsAdmin { get; set; } = false;
        public bool IsModerator { get; set; } = false;

        // MapInstance
        public int Port { get; set; }
        public string ServerIP { get; set; }
        public int MapInstanceID { get; set; }
        
        // Class
        public string ClassName { get; set; }
    }
}
