using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OWSData.Models.Tables
{
    public partial class PlayerGroupTypes
    {
        public PlayerGroupTypes()
        {
            PlayerGroup = new HashSet<PlayerGroup>();
        }

        public int PlayerGroupTypeId { get; set; }
        public string PlayerGroupTypeDesc { get; set; }

        public ICollection<PlayerGroup> PlayerGroup { get; set; }
    }
}
