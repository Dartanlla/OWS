using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OWSData.Repositories.Interfaces;
using OWSShared.Extensions;
using OWSShared.Implementations;
using OWSShared.Interfaces;
using OWSShared.Messages;
using OWSShared.Objects;
using SimpleInjector;

namespace OWSInstanceLauncher
{
    public class Startup
    {
        //Container container;
        private Container container = new SimpleInjector.Container();
        private OWSData.Models.OWSInstanceLauncherOptions owsInstanceLauncherOptions;

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            container.Options.ResolveUnregisteredConcreteTypes = false;

            Configuration = configuration;

            owsInstanceLauncherOptions = new OWSData.Models.OWSInstanceLauncherOptions();
            Configuration.GetSection(OWSData.Models.OWSInstanceLauncherOptions.SectionName).Bind(owsInstanceLauncherOptions);

            //Check appsettings.json file for potential errors
            bool thereWasAStartupError = false;

            Console.ForegroundColor = ConsoleColor.Red;
            //Abort if there is not a valid OWSAPIKey in appsettings.json
            if (String.IsNullOrEmpty(owsInstanceLauncherOptions.OWSAPIKey))
            {
                thereWasAStartupError = true;
                Console.WriteLine("Please enter a valid OWSAPIKey in appsettings.json!");
            }
            //Abort if there is not a valid PathToDedicatedServer in appsettings.json
            else if (String.IsNullOrEmpty(owsInstanceLauncherOptions.PathToDedicatedServer))
            {
                thereWasAStartupError = true;
                Console.WriteLine("Please enter a valid PathToDedicatedServer in appsettings.json!");
            }
            //Abort if there is not a valid ServerArguments in appsettings.json
            else if (String.IsNullOrEmpty(owsInstanceLauncherOptions.ServerArguments))
            {
                thereWasAStartupError = true;
                Console.WriteLine("Please enter a valid ServerArguments in appsettings.json!");
            }
            //Check that a file exists at PathToDedicatedServer
            else if (!File.Exists(owsInstanceLauncherOptions.PathToDedicatedServer))
            {
                thereWasAStartupError = true;
                Console.WriteLine("Your PathToDedicatedServer in appsettings.json points to a file that does not exist!  Please either point PathToDedicatedServer to your UE4 Editor exe or to your packaged UE4 dedicated server exe!");
            }
            //If using the UE4 editor, make sure there is a project path in Server Arguments
            else if (owsInstanceLauncherOptions.PathToDedicatedServer.Contains("UE4Editor.exe"))
            {
                string serverArgumentsProjectPattern = @"^"".*.uproject"" ";
                MatchCollection testForUprojectPath = Regex.Matches(owsInstanceLauncherOptions.ServerArguments, serverArgumentsProjectPattern);
                if (testForUprojectPath.Count == 1)
                {
                    Match testForUprojectPathMatch = testForUprojectPath.First();
                    string foundUprojectPath = testForUprojectPathMatch.Value;
                    string foundUprojectPathToValidate = foundUprojectPath.Trim().Replace("\"", "");

                    if (!File.Exists(foundUprojectPathToValidate))
                    {
                        thereWasAStartupError = true;
                        Console.WriteLine("Your ServerArguments in appsettings.json points to a uproject file that does not exist!");
                    }
                    else if (!owsInstanceLauncherOptions.ServerArguments.Contains("{0}"))
                    {
                        thereWasAStartupError = true;
                        Console.WriteLine("Your ServerArguments in appsettings.json is missing the {0} parameter.  See the sample appsettings.json for proper format!");
                    }
                    else if (!owsInstanceLauncherOptions.ServerArguments.Contains("{1}"))
                    {
                        thereWasAStartupError = true;
                        Console.WriteLine("Your ServerArguments in appsettings.json is missing the {1} parameter.  See the sample appsettings.json for proper format!");
                    }
                }
                else
                {
                    thereWasAStartupError = true;
                    Console.WriteLine("Because you are using UE4Editor.exe, your Server Arguments in appsettings.json must contain a path to the uproject file.  See the sample appsettings.json for proper format!");
                }
            }            
            Console.ForegroundColor = ConsoleColor.White;

            //If there was a startup error, don't continue any further.  Wait for shutdown.
            if (thereWasAStartupError)
            {
                Console.WriteLine("Error encountered.  Shutting down...");
                Environment.Exit(-1);
            }
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();

            services.AddMvcCore(config =>
            {
                //IHttpRequestStreamReaderFactory readerFactory = services.BuildServiceProvider().GetRequiredService<IHttpRequestStreamReaderFactory>();
                //config.ModelBinderProviders.Insert(0, new Microsoft.AspNetCore.Mvc.ModelBinding.Binders.BodyModelBinderProvider(config.InputFormatters, readerFactory));
                //config.ModelBinderProviders.Insert(0, new QueryModelBinderProvider(container));
            })
            .AddViews()
            .AddApiExplorer();

            services.ConfigureWritable<OWSData.Models.OWSInstanceLauncherOptions>(Configuration.GetSection(OWSData.Models.OWSInstanceLauncherOptions.SectionName));
            services.Configure<OWSShared.Options.APIPathOptions>(Configuration.GetSection(OWSShared.Options.APIPathOptions.SectionName));
            services.Configure<OWSShared.Options.RabbitMQOptions>(Configuration.GetSection(OWSShared.Options.RabbitMQOptions.SectionName));

            var apiPathOptions = new OWSShared.Options.APIPathOptions();
            Configuration.GetSection(OWSShared.Options.APIPathOptions.SectionName).Bind(apiPathOptions);

            services.AddHttpClient("OWSInstanceManagement", c =>
            {
                c.BaseAddress = new Uri(apiPathOptions.InternalInstanceManagementApiURL);
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                c.DefaultRequestHeaders.Add("User-Agent", "OWSInstanceLauncher");
                c.DefaultRequestHeaders.Add("X-CustomerGUID", owsInstanceLauncherOptions.OWSAPIKey);
            });

            services.AddSimpleInjector(container, options => {
                options.AddHostedService<TimedHostedService<IInstanceLauncherJob>>();
                options.AddHostedService<TimedHostedService<IServerHealthMonitoringJob>>();
            });

            InitializeContainer(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSimpleInjector(container);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });

            container.Verify();
        }

        private void InitializeContainer(IServiceCollection services)
        {
            //Register our ZoneServerProcessesRepository to store a list of our running zone server processes for this hardware device
            //container.Register<IZoneServerProcessesRepository, OWSData.Repositories.Implementations.InMemory.ZoneServerProcessesRepository>(Lifestyle.Singleton);
            container.RegisterSingleton<IZoneServerProcessesRepository, OWSData.Repositories.Implementations.InMemory.ZoneServerProcessesRepository>();

            //Register our OWSInstanceLauncherDataRepository to store information that needs to be shared amongst the jobs
            //container.Register<IOWSInstanceLauncherDataRepository, OWSData.Repositories.Implementations.InMemory.OWSInstanceLauncherDataRepository> (Lifestyle.Singleton);
            container.RegisterSingleton<IOWSInstanceLauncherDataRepository, OWSData.Repositories.Implementations.InMemory.OWSInstanceLauncherDataRepository>();

            //ServerLauncherMQListener runs only once
            container.RegisterInstance(new TimedHostedService<IInstanceLauncherJob>.Settings(
                interval: TimeSpan.FromSeconds(10),
                runOnce: true,
                action: processor => processor.DoWork(),
                dispose: processor => processor.Dispose()
            ));

            //ServerLauncherHealthMonitoring runs once every X seconds.  X is configured in the OWSInstanceLauncherOptions in appsettings.json
            container.RegisterInstance(new TimedHostedService<IServerHealthMonitoringJob>.Settings(
                interval: TimeSpan.FromSeconds(owsInstanceLauncherOptions.RunServerHealthMonitoringFrequencyInSeconds),
                runOnce: false,
                action: processor => processor.DoWork(),
                dispose: processor => processor.Dispose()
            ));

            //Register our Server Launcher MQ Listener job
            container.RegisterSingleton<IInstanceLauncherJob, ServerLauncherMQListener>();

            //Register our Server Launcher Health Monitoring Job
            container.RegisterSingleton<IServerHealthMonitoringJob, ServerLauncherHealthMonitoring>();

            var provider = services.BuildServiceProvider();
            container.RegisterInstance<IServiceProvider>(provider);
        }

    }
}
