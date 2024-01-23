﻿using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    [Serializable]
    public record Maps(
        Guid CustomerGuid,
        int MapId,
        string MapName,
        byte[] MapData,
        short Width,
        short Height,
        string ZoneName,
        string WorldCompContainsFilter,
        string WorldCompListFilter,
        int MapMode,
        int SoftPlayerCap,
        int HardPlayerCap
        );

    //public partial class Maps
    //{
    //    public Guid CustomerGuid { get; set; }
    //    public int MapId { get; set; }
    //    public string MapName { get; set; }
    //    public byte[] MapData { get; set; }
    //    public short Width { get; set; }
    //    public short Height { get; set; }
    //    public string ZoneName { get; set; }
    //    public string WorldCompContainsFilter { get; set; }
    //    public string WorldCompListFilter { get; set; }
    //    public int MapMode { get; set; }
    //    public int SoftPlayerCap { get; set; }
    //    public int HardPlayerCap { get; set; }
    //}
}
