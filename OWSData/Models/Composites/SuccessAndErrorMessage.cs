using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSData.Models.Composites
{
    public class SuccessAndErrorMessage
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}
