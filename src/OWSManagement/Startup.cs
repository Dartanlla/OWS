using Microsoft.AspNetCore.DataProtection;
using Microsoft.OpenApi.Models;
using OWSShared.Implementations;
using OWSShared.Interfaces;
using OWSShared.Middleware;
using System.Net.Http.Headers;
using SimpleInjector;
using VueCliMiddleware;

namespace OWSManagement
{
    public class Startup
    {
        private Container container = new SimpleInjector.Container();

        public Startup(IConfiguration configuration)
        {
            container.Options.ResolveUnregisteredConcreteTypes = false;

            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {

            services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo("./temp/DataProtection-Keys"));

            services.AddMemoryCache();

            services.AddHttpContextAccessor();

            services.AddControllers();

            services.AddSimpleInjector(container, options => {
                options.AddAspNetCore()
                    .AddControllerActivation();
            });

            services.Configure<OWSShared.Options.ManagementOptions>(Configuration.GetSection(OWSShared.Options.ManagementOptions.SectionName));
            services.Configure<OWSShared.Options.APIPathOptions>(Configuration.GetSection(OWSShared.Options.APIPathOptions.SectionName));

            var owsManagementOptions = new OWSShared.Options.ManagementOptions();
            Configuration.GetSection(OWSShared.Options.ManagementOptions.SectionName).Bind(owsManagementOptions);

            var apiPathOptions = new OWSShared.Options.APIPathOptions();
            Configuration.GetSection(OWSShared.Options.APIPathOptions.SectionName).Bind(apiPathOptions);

            services.AddSpaStaticFiles(options => options.RootPath = "wwwroot/dist");

            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Open World Server Management API", Version = "v1" });

                c.AddSecurityDefinition("X-CustomerGUID", new OpenApiSecurityScheme
                {
                    Description = "Authorization header using the X-CustomerGUID scheme",
                    Name = "X-CustomerGUID",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "X-CustomerGUID"
                });

                c.OperationFilter<SwaggerSecurityRequirementsDocumentFilter>();

                var filePath = Path.Combine(System.AppContext.BaseDirectory, "OWSManagement.xml");
                c.IncludeXmlComments(filePath);
            });

            services.AddHttpClient("OWSPublicApi", c =>
            {
                c.BaseAddress = new Uri(apiPathOptions.InternalPublicApiURL);
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                c.DefaultRequestHeaders.Add("User-Agent", "OWSManagement");
                c.DefaultRequestHeaders.Add("X-CustomerGUID", owsManagementOptions.OWSAPIKey);
            });

            InitializeContainer(services);

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            app.UseSimpleInjector(container);

            app.UseMiddleware<StoreCustomerGUIDMiddleware>(container);

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("./v1/swagger.json", "Open World Server Management API");
            });

            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "wwwroot";
                if (env.IsDevelopment())
                {
                    spa.UseProxyToSpaDevelopmentServer("http://localhost:5001");
                    spa.UseVueCli(npmScript: "serve", port: 5001);
                }
            });
            
            container.Verify();
        }

        private void InitializeContainer(IServiceCollection services)
        {
            container.Register<IHeaderCustomerGUID, HeaderCustomerGUID>(Lifestyle.Scoped);
        }
    }
}
