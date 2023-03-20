using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OWSShared.Implementations
{
    public class DefaultGetReadOnlyPublicCharacterData : IGetReadOnlyPublicCharacterData
    {
        public Task<string> GetReadOnlyPublicCharacterData(int characterId)
        {
            return Task.FromResult(string.Empty);
        }
    }
}
