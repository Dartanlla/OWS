using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    [Serializable]
    public record PlayerGroupCharacters(
        int PlayerGroupId,
        Guid CustomerGuid,
        int CharacterId,
        DateTime DateAdded,
        int TeamNumber
        );

    //public partial class PlayerGroupCharacters
    //{
    //    public int PlayerGroupId { get; set; }
    //    public Guid CustomerGuid { get; set; }
    //    public int CharacterId { get; set; }
    //    public DateTime DateAdded { get; set; }
    //    public int TeamNumber { get; set; }
    //}
}
