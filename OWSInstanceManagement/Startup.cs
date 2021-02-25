using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OWSData.Repositories.Interfaces;
using OWSShared.Implementations;
using OWSShared.Interfaces;
using OWSShared.Middleware;
using SimpleInjector;

namespace OWSInstanceManagement
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
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvcCore(config =>
            {
                config.EnableEndpointRouting = false;
                //IHttpRequestStreamReaderFactory readerFactory = services.BuildServiceProvider().GetRequiredService<IHttpRequestStreamReaderFactory>();
                //config.ModelBinderProviders.Insert(0, new Microsoft.AspNetCore.Mvc.ModelBinding.Binders.BodyModelBinderProvider(config.InputFormatters, readerFactory));
                //config.ModelBinderProviders.Insert(0, new QueryModelBinderProvider(container));
            })
            .AddViews()
            .AddApiExplorer()
            .AddNewtonsoftJson();

            services.AddSimpleInjector(container, options => {
                options.AddAspNetCore()
                    .AddControllerActivation()
                    .AddViewComponentActivation();
                //.AddPageModelActivation()
                //.AddTagHelperActivation();
            });

            services.Configure<OWSData.Models.StorageOptions>(Configuration.GetSection(OWSData.Models.StorageOptions.SectionName));
            services.Configure<OWSShared.Options.APIPathOptions>(Configuration.GetSection(OWSShared.Options.APIPathOptions.SectionName));

            InitializeContainer(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSimpleInjector(container);

            app.UseMiddleware<StoreCustomerGUIDMiddleware>(container);

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

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            //app.UseAuthorization();

            app.UseMvc();

            container.Verify();
        }

        private void InitializeContainer(IServiceCollection services)
        {
            container.Register<IInstanceManagementRepository, OWSData.Repositories.Implementations.MSSQL.InstanceManagementRepository>(Lifestyle.Scoped);
            container.Register<ICharactersRepository, OWSData.Repositories.Implementations.MSSQL.CharactersRepository>(Lifestyle.Scoped);
            container.Register<IUsersRepository, OWSData.Repositories.Implementations.MSSQL.UsersRepository>(Lifestyle.Scoped);
            container.Register<IHeaderCustomerGUID, HeaderCustomerGUID>(Lifestyle.Scoped);

            var provider = services.BuildServiceProvider();
            container.RegisterInstance<IServiceProvider>(provider);
        }
    }
}
