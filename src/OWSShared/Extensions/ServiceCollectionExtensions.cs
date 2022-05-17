using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OWSShared.Implementations;
using OWSShared.Interfaces;
using System;
using System.Linq;

namespace OWSShared.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureAndValidate<T>(this IServiceCollection @this, IConfiguration config) where T : class => @this
            .Configure<T>(config)
            .PostConfigure<T>(settings =>
            {
                var configErrors = settings.ValidationErrors().ToArray();
                if (configErrors.Any())
                {
                    var aggrErrors = string.Join(",", configErrors);
                    var count = configErrors.Length;
                    var configType = typeof(T).Name;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Found {count} configuration error(s) in {configType}: {aggrErrors}");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            });

        public static IServiceCollection ConfigureAndValidate<T>(this IServiceCollection @this, string name, IConfiguration config) where T : class => @this
            .Configure<T>(name, config)
            .PostConfigure<T>(name, settings =>
            {
                var configErrors = settings.ValidationErrors().ToArray();
                if (configErrors.Any())
                {
                    var aggrErrors = string.Join(",", configErrors);
                    var count = configErrors.Length;
                    var configType = typeof(T).Name;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Found {count} configuration error(s) in {configType}: {aggrErrors}");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            });

        public static void ConfigureWritable<T>(this IServiceCollection services, IConfigurationSection section, string file = "appsettings.json") where T : class, new()
        {
            services.Configure<T>(section);
            services.AddSingleton<IWritableOptions<T>>(provider =>
            {
                var configuration = (IConfigurationRoot)provider.GetService<IConfiguration>();
                var environment = provider.GetService<IWebHostEnvironment>();
                var options = provider.GetService<IOptionsMonitor<T>>();
                return new WritableOptions<T>(environment, options, configuration, section.Key, file);
            });
        }
    }
}
