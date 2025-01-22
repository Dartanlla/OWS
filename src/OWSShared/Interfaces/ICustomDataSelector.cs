using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OWSShared.Interfaces
{
    public interface ICustomDataSelector
    {
        bool ShouldExportThisCustomDataField(string fieldName);
    }
}
