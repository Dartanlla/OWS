using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    public partial class DefaultCustomData
    {
     //   public Guid CustomerGuid { get; set; }
      //  public int DefaultCustomCharacterDataId { get; set; }
      //  public int DefaultCharacterValuesId { get; set; }
        public string CustomFieldName { get; set; }
        public string FieldValue { get; set; }

    }
}
