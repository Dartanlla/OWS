using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OWSData.Repositories.Interfaces;
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

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            container.Options.ResolveUnregisteredConcreteTypes = false;

            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();

            services.AddMvcCore(config => {
                //IHttpRequestStreamReaderFactory readerFactory = services.BuildServiceProvider().GetRequiredService<IHttpRequestStreamReaderFactory>();
                //config.ModelBinderProviders.Insert(0, new Microsoft.AspNetCore.Mvc.ModelBinding.Binders.BodyModelBinderProvider(config.InputFormatters, readerFactory));
                //config.ModelBinderProviders.Insert(0, new QueryModelBinderProvider(container));
            })
            .AddViews()
            .AddApiExplorer()
            .AddNewtonsoftJson();
            //.AddCors()
            /*.AddJsonFormatters()
            .AddJsonOptions(options => {
                options.SerializerSettings.Converters.Add(new RequestHandlerConverter<IRequest>(container));
            });*/

            services.Configure<OWSData.Models.OWSInstanceLauncherOptions>(Configuration.GetSection("OWSInstanceLauncherOptions"));

            services.AddHttpClient("OWSInstanceManagement", c =>
            {
                c.BaseAddress = new Uri("https://localhost:44329/");
                c.DefaultRequestHeaders.Add("Accept", "application/json");
                c.DefaultRequestHeaders.Add("User-Agent", "OWSInstanceLauncher");
            });

            services.AddSimpleInjector(container, options => {
                options.AddHostedService<TimedHostedService<IInstanceLauncherJob>>();
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
            container.Register<IZoneServerProcessesRepository, OWSData.Repositories.Implementations.InMemory.ZoneServerProcessesRepository>(Lifestyle.Singleton);
            container.RegisterInstance(new TimedHostedService<IInstanceLauncherJob>.Settings(
                interval: TimeSpan.FromSeconds(10),
                runOnce: true,
                action: processor => processor.DoWork()));

            container.Register<IInstanceLauncherJob, ServerLauncherMQListener>();

            var provider = services.BuildServiceProvider();
            container.RegisterInstance<IServiceProvider>(provider);
        }
    }
}
