using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OWSShared.Interfaces
{
    public interface IRequestHandler<request, response> //where request : Request
    {
        Task<response> Handle();
    }

    public interface IRequestHandler<request> //where request : Request
    {
        Task Handle();
    }
}
