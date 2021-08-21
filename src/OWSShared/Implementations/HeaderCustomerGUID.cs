using System;
using System.Collections.Generic;
using System.Text;
using OWSShared.Interfaces;

namespace OWSShared.Implementations
{
    public class HeaderCustomerGUID : IHeaderCustomerGUID
    {
        public Guid CustomerGUID { get; set; }
    }
}
