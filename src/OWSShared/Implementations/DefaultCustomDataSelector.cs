using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OWSShared.Implementations
{
    public class DefaultCustomDataSelector : ICustomDataSelector
    {
        public bool ShouldExportThisCustomDataField(string fieldName)
        {
            return fieldName switch
            {
                "BaseCharacterStats" => true,
                "CharacterExperience" => true,
                _ => false,
            };
        }
    }
}
