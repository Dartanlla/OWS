using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace OWSCharacterPersistence
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch
            {
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog((hostContext, serviceProvider, loggerConfiguration) =>
                {
                    loggerConfiguration
                        .ReadFrom.Configuration(hostContext.Configuration)
                        .ReadFrom.Services(serviceProvider);
                })
                .ConfigureLogging((hostContext, builder) =>
                {
                    builder.ClearProviders();
                    builder.AddConsole();
                    builder.AddSerilog();
                })
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}
