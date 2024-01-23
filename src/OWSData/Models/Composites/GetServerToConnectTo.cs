using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace OWSData.Models.Composites
{
    public record GetServerToConnectTo(IPAddress ServerIP, int Port, int MapInstanceID, bool Success = true, string ErrorMessage = "");

    //public class GetServerToConnectTo
    //{
    //    public bool Success { get; set; }
    //    public string ErrorMessage { get; set; }

    //    public string ServerIP { get; set; }
    //    public int Port { get; set; }
    //    public int MapInstanceID { get; set; }
    //}
}
