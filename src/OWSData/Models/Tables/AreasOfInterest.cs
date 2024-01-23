﻿using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    [Serializable]
    public record AreasOfInterest(
        Guid CustomerGuid,
        Guid AreasOfInterestGuid,
        int MapZoneId,
        string AreaOfInterestName,
        double Radius,
        int AreaOfInterestType,
        double? X,
        double? Y,
        double? Z,
        double? Rx,
        double? Ry,
        double? Rz,
        string CustomData
        );

    //public partial class AreasOfInterest
    //{
    //    public Guid CustomerGuid { get; set; }
    //    public Guid AreasOfInterestGuid { get; set; }
    //    public int MapZoneId { get; set; }
    //    public string AreaOfInterestName { get; set; }
    //    public double Radius { get; set; }
    //    public int AreaOfInterestType { get; set; }
    //    public double? X { get; set; }
    //    public double? Y { get; set; }
    //    public double? Z { get; set; }
    //    public double? Rx { get; set; }
    //    public double? Ry { get; set; }
    //    public double? Rz { get; set; }
    //    public string CustomData { get; set; }
    //}
}


