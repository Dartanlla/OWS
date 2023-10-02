using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OWSData.Models.Tables
{
    public partial class PlayerGroup
    {
        public int PlayerGroupId { get; set; }
        public Guid CustomerGuid { get; set; }
        public int PlayerGroupTypeId { get; set; }
        public int ReadyState { get; set; }
        public DateTime? DateAdded { get; set; }
    }
}
