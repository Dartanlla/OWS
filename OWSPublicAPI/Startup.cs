﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using SimpleInjector.Integration.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.AspNetCore.Authentication;
using System.Text.Encodings.Web;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OWSData.Repositories.Interfaces;
using OWSData.Repositories.Implementations;
using OWSPublicAPI.Requests;
using OWSShared.Interfaces;
using OWSShared.Implementations;
using OWSShared.Middleware;
using OWSExternalLoginProviders.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace OWSPublicAPI
{
    public class Startup
    {
        //Container container;
        private Container container = new SimpleInjector.Container();

        public Startup(IConfiguration configuration)
        {
            container.Options.ResolveUnregisteredConcreteTypes = false;
            //container = new Container();

            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddMvc();

            services.AddMvcCore(config => {
                config.EnableEndpointRouting = false;
                //IHttpRequestStreamReaderFactory readerFactory = services.BuildServiceProvider().GetRequiredService<IHttpRequestStreamReaderFactory>();
                //config.ModelBinderProviders.Insert(0, new Microsoft.AspNetCore.Mvc.ModelBinding.Binders.BodyModelBinderProvider(config.InputFormatters, readerFactory));
                //config.ModelBinderProviders.Insert(0, new QueryModelBinderProvider(container));
            })
            .AddViews()
            .AddApiExplorer()
            //.AddCors()
            //.AddJsonFormatters()
            /*.AddJsonOptions(options => {
                options.SerializerSettings.Converters.Add(new RequestHandlerConverter<IRequest>(container));
            })*/
            .AddNewtonsoftJson(o =>
            {
                //o.SerializerSettings.Converters.Add(new RequestHandlerConverter<IRequest>(container));
            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            services.AddSimpleInjector(container, options => {
                options.AddAspNetCore()
                    .AddControllerActivation()
                    .AddViewComponentActivation();
                    //.AddPageModelActivation()
                    //.AddTagHelperActivation();
            });

            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Open World Server Authentication API", Version = "v1" });
                //c.OperationFilter<SwaggerSecurityRequirementsDocumentFilter>();  //Dart - Removed as this breaks the new version of Swagger

                c.AddSecurityDefinition("X-CustomerGUID", new OpenApiSecurityScheme()
                {
                    Description = "Authorization header using the X-CustomerGUID scheme",
                    Name = "X-CustomerGUID",
                    In = ParameterLocation.Header
                });
                
                var filePath = Path.Combine(System.AppContext.BaseDirectory, "OWSPublicAPI.xml");
                c.IncludeXmlComments(filePath);
            });

            var apiPathOptions = new OWSShared.Options.APIPathOptions();
            Configuration.GetSection(OWSShared.Options.APIPathOptions.SectionName).Bind(apiPathOptions);

            services.AddHttpClient("OWSInstanceManagement", c =>
            {
                c.BaseAddress = new Uri(apiPathOptions.InternalInstanceManagementApiURL);
                c.DefaultRequestHeaders.Add("Accept", "application/json");
                c.DefaultRequestHeaders.Add("User-Agent", "OWSPublicAPI");
            });

            services.Configure<OWSShared.Options.PublicAPIOptions>(Configuration.GetSection(OWSShared.Options.PublicAPIOptions.SectionName));
            services.Configure<OWSShared.Options.APIPathOptions>(Configuration.GetSection(OWSShared.Options.APIPathOptions.SectionName));
            services.Configure<OWSData.Models.StorageOptions>(Configuration.GetSection(OWSData.Models.StorageOptions.SectionName));
            services.Configure<OWSExternalLoginProviders.Options.ExternalLoginProviderOptions>(Configuration.GetSection(OWSExternalLoginProviders.Options.ExternalLoginProviderOptions.SectionName));

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
                app.UseHsts();
            }

            //app.UseHttpsRedirection();

            //app.UseAuthentication();

            //app.UseStaticFiles();
            app.UseRouting();
            app.UseMvc();

            app.UseSwagger(/*c =>
            {
                c.RouteTemplate =
                    "api-docs/{documentName}/swagger.json";
            }*/);
            app.UseSwaggerUI(c => 
            {
                //c.RoutePrefix = "api-docs";
                c.SwaggerEndpoint("./v1/swagger.json", "Open World Server Authentication API");
            });

            container.Verify();
        }

        private void InitializeContainer(IServiceCollection services)
        {
            container.Register<ICharactersRepository, OWSData.Repositories.Implementations.MSSQL.CharactersRepository>(Lifestyle.Transient);
            container.Register<IUsersRepository, OWSData.Repositories.Implementations.MSSQL.UsersRepository>(Lifestyle.Transient);
            container.Register<IHeaderCustomerGUID, HeaderCustomerGUID>(Lifestyle.Scoped);

            //Register Xsolla
            container.Register<IExternalLoginProvider, OWSExternalLoginProviders.Implementations.XsollaLoginProvider>(Lifestyle.Scoped);
            
            var provider = services.BuildServiceProvider();
            container.RegisterInstance<IServiceProvider>(provider);

            /*
            //Doesn't do anything
            var requestAssembly = typeof(IRequest).GetTypeInfo().Assembly;
            container.Collection.Register(typeof(IRequest), new[] { requestAssembly });
            */

            //Doesn't work
            //container.Register(typeof(IRequestHandler<,>), new[] { typeof(IRequestHandler<,>).Assembly });

            //Doesn't work
            //var requestHandlerAssembly = typeof(IRequestHandler<,>).GetTypeInfo().Assembly;
            //container.Collection.Register(typeof(IRequestHandler<,>), new[] { requestHandlerAssembly });

            /*
            //These work, but are too slow
            container.Register<Requests.Characters.GetByNameRequest>();
            container.Register<Requests.Users.LoginAndCreateSessionRequest>();
            container.Register<Requests.Users.GetUserSessionRequest>();
            container.Register<Requests.Users.GetServerToConnectToRequest>();
            container.Register<Requests.Users.UserSessionSetSelectedCharacterRequest>();
            */
        }
    }
}
