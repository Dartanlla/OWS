using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    [Serializable]
    public record CustomCharacterData(
        Guid CustomerGuid,
        int CustomCharacterDataId,
        int CharacterId,
        string CustomFieldName,
        string FieldValue
        );

    //public partial class CustomCharacterData
    //{
    //    public Guid CustomerGuid { get; set; }
    //    public int CustomCharacterDataId { get; set; }
    //    public int CharacterId { get; set; }
    //    public string CustomFieldName { get; set; }
    //    public string FieldValue { get; set; }

    //}
}
