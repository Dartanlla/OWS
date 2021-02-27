using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OWSPublicAPI
{
    /// <summary>
    /// OWS Public API Program
    /// </summary>
    /// <remarks>
    /// The program class.
    /// </remarks>
    public class Program
    {
        /// <summary>
        /// OWS Public API Main
        /// </summary>
        /// <remarks>
        /// The program entry point.
        /// </remarks>
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// OWS Public API CreateHostBuilder
        /// </summary>
        /// <remarks>
        /// Configure the web host.
        /// </remarks>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
