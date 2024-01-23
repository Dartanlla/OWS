using OWSData.Models.Tables;
using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Models.Composites
{
    [Serializable]
    public record CustomCharacterDataRows(IEnumerable<CustomCharacterData> Rows);

    //public class CustomCharacterDataRows
    //{
    //    public IEnumerable<CustomCharacterData> Rows { get; set; }
    //}
}
