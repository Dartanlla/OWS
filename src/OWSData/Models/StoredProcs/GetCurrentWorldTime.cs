using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Models.StoredProcs
{
    [Serializable]
    public record GetCurrentWorldTime(long CurrentWorldTime);

    //public class GetCurrentWorldTime
    //{
    //    public long CurrentWorldTime { get; set; }
    //}
}
