using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    [Serializable]
    public record Races(
        Guid CustomerGuid,
        int RaceId,
        string RaceName
        );

    //public partial class Races
    //{
    //    public Guid CustomerGuid { get; set; }
    //    public int RaceId { get; set; }
    //    public string RaceName { get; set; }
    //}
}
