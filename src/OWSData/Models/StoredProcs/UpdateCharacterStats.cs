using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Models.StoredProcs
{
    public class UpdateCharacterStats
    {
        public string StatIdentifier { get; set; }
        public float Value { get; set; }
    }
}
