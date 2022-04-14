using System;
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
using OWSShared.Extensions;
using SimpleInjector;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

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
            services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo("./temp/DataProtection-Keys"));

            services.AddMvcCore(config =>
            {
                config.EnableEndpointRouting = false;
                //IHttpRequestStreamReaderFactory readerFactory = services.BuildServiceProvider().GetRequiredService<IHttpRequestStreamReaderFactory>();
                //config.ModelBinderProviders.Insert(0, new Microsoft.AspNetCore.Mvc.ModelBinding.Binders.BodyModelBinderProvider(config.InputFormatters, readerFactory));
                //config.ModelBinderProviders.Insert(0, new QueryModelBinderProvider(container));
            })
            .AddViews()
            .AddApiExplorer();

            services.AddSimpleInjector(container, options => {
                options.AddAspNetCore()
                    .AddControllerActivation()
                    .AddViewComponentActivation();
                //.AddPageModelActivation()
                //.AddTagHelperActivation();
            });

            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Open World Server Instance Management API", Version = "v1" });

                c.AddSecurityDefinition("X-CustomerGUID", new OpenApiSecurityScheme
                {
                    Description = "Authorization header using the X-CustomerGUID scheme",
                    Name = "X-CustomerGUID",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "X-CustomerGUID"
                });

                c.OperationFilter<SwaggerSecurityRequirementsDocumentFilter>();

                var filePath = Path.Combine(System.AppContext.BaseDirectory, "OWSInstanceManagement.xml");
                c.IncludeXmlComments(filePath);
            });

            services.Configure<OWSData.Models.StorageOptions>(Configuration.GetSection(OWSData.Models.StorageOptions.SectionName));
            services.Configure<OWSShared.Options.APIPathOptions>(Configuration.GetSection(OWSShared.Options.APIPathOptions.SectionName));
            services.Configure<OWSShared.Options.RabbitMQOptions>(Configuration.GetSection(OWSShared.Options.RabbitMQOptions.SectionName));
            //Ben added code for docker real ip
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("::ffff:172.19.0.0"), 12));
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                           ForwardedHeaders.XForwardedProto;
            });

            services.AddCustomHealthCheck(Configuration);

            InitializeContainer(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //Ben added code
            app.UseForwardedHeaders();
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

            app.UseCustomHealthCheck();

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
                c.SwaggerEndpoint("./v1/swagger.json", "Open World Server Instance Management API");
            });

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
