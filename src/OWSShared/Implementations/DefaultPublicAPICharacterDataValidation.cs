using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OWSShared.Implementations
{
    public class DefaultPublicAPICharacterDataValidation : IPublicAPICharacterDataValidation
    {
        /***** WARNING: This default implementation has NO validation and is not safe to run in production! *****/
        public Task<bool> ValidatePublicAPICharacterData(string characterDataJson)
        {
            return Task.FromResult<bool>(true);
        }
    }
}
