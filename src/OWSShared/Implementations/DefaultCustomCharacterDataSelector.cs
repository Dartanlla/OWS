using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OWSShared.Implementations
{
    public class DefaultCustomCharacterDataSelector : ICustomCharacterDataSelector
    {
        public bool ShouldExportThisCustomCharacterDataField(string fieldName)
        {
            if (fieldName == "CosmeticCharacterData")
            {
                return true;
            }

            return false;
        }
    }
}
