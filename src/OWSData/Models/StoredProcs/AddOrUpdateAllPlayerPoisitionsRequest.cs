using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Models.StoredProcs
{
    [Serializable]
    public record AddOrUpdateCustomCharacterData(string CharacterName, string CustomFieldName, string FieldValue);

    //public class AddOrUpdateCustomCharacterData
    //{
    //    public string CharacterName { get; set; }
    //    public string CustomFieldName { get; set; }
    //    public string FieldValue { get; set; }
    //}
}