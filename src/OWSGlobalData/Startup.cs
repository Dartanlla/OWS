﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using OWSData.Repositories.Interfaces;
using OWSShared.Implementations;
using OWSShared.Interfaces;
using OWSShared.Middleware;
using SimpleInjector;

namespace OWSGlobalData
{
    public class Startup
    {
        //Container container;
        private Container container = new SimpleInjector.Container();

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            container.Options.ResolveUnregisteredConcreteTypes = false;

            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo("./temp/DataProtection-Keys"));

            services.AddHttpContextAccessor();

            services.AddMvcCore(config =>
            {
                config.EnableEndpointRouting = false;
            })
            .AddViews()
            .AddApiExplorer();

            services.AddSimpleInjector(container, options => {
                options.AddAspNetCore()
                    .AddControllerActivation()
                    .AddViewComponentActivation();
            });

            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Open World Server Global Data API", Version = "v1" });

                c.AddSecurityDefinition("X-CustomerGUID", new OpenApiSecurityScheme
                {
                    Description = "Authorization header using the X-CustomerGUID scheme",
                    Name = "X-CustomerGUID",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "X-CustomerGUID"
                });

                c.OperationFilter<SwaggerSecurityRequirementsDocumentFilter>();

                var filePath = Path.Combine(System.AppContext.BaseDirectory, "OWSGlobalData.xml");
                c.IncludeXmlComments(filePath);
            });

            services.Configure<OWSData.Models.StorageOptions>(Configuration.GetSection(OWSData.Models.StorageOptions.SectionName));
            services.Configure<OWSShared.Options.APIPathOptions>(Configuration.GetSection(OWSShared.Options.APIPathOptions.SectionName));
            services.Configure<OWSShared.Options.RabbitMQOptions>(Configuration.GetSection(OWSShared.Options.RabbitMQOptions.SectionName));

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

            app.UseSwagger(/*c =>
            {
                c.RouteTemplate =
                    "api-docs/{documentName}/swagger.json";
            }*/);
            app.UseSwaggerUI(c =>
            {
                //c.RoutePrefix = "api-docs";
                c.SwaggerEndpoint("./v1/swagger.json", "Open World Server Global Data API");
            });

            container.Verify();
        }

        private void InitializeContainer(IServiceCollection services)
        {
            var OWSStorageConfig = Configuration.GetSection("OWSStorageConfig");
            if (OWSStorageConfig.Exists())
            {
                string dbBackend = OWSStorageConfig.GetValue<string>("OWSDBBackend");

                switch (dbBackend)
                {
                    default: // Default to MSSQL
                        container.Register<IGlobalDataRepository, OWSData.Repositories.Implementations.MSSQL.GlobalDataRepository>(Lifestyle.Scoped);
                        break;
                }
            }
            container.Register<IHeaderCustomerGUID, HeaderCustomerGUID>(Lifestyle.Scoped);

            var provider = services.BuildServiceProvider();
            container.RegisterInstance<IServiceProvider>(provider);
        }
    }
}
