using System;
using System.Collections.Generic;
using System.Text;

namespace OWSShared.Interfaces
{
    public interface IInstanceLauncherJob
    {
        void DoWork();
        void Dispose();
    }
}
