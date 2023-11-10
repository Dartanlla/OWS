using Microsoft.AspNetCore.Mvc.Formatters;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OWSShared.Implementations
{
    public class HubWorldMMOCosmeticCharacterData
    {
        public int Gender {  get; set; }
        public string HairColor { get; set; }
    }

    public class HubWorldMMOPublicAPICharacterDataValidation : IPublicAPICharacterDataValidation
    {
        public Task<bool> ValidatePublicAPICharacterData(string characterDataJson)
        {
            HubWorldMMOCosmeticCharacterData hubWorldMMOCosmeticCharacterData = System.Text.Json.JsonSerializer.Deserialize<HubWorldMMOCosmeticCharacterData>(characterDataJson);

            //Check that Gender
            if (hubWorldMMOCosmeticCharacterData.Gender < 0 || hubWorldMMOCosmeticCharacterData.Gender > 1)
            {
                return Task.FromResult(false);
            }
            //Check the Hair Color

            return Task.FromResult(true);
        }
    }
}
