using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using OWSData.Health;
using OWSData.Repositories.Interfaces;
using OWSManagement.Auth;
using OWSShared.Interfaces;
using SimpleInjector;
using System;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;

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
            // The keys sign the console's session cookies; keep them on a volume so a container restart keeps everyone signed in.
            services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo("./temp/DataProtection-Keys"));
            services.AddHealthChecks().AddOWSPostgres(Configuration);

            services.AddMemoryCache();

            services.AddHttpContextAccessor();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = ConsoleAuth.CookieName;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.ExpireTimeSpan = TimeSpan.FromHours(12);
                    options.SlidingExpiration = true;
                    options.Events.OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    };
                    options.Events.OnRedirectToAccessDenied = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    };
                    options.Events.OnValidatePrincipal = context => ConsoleAuth.ValidatePrincipal(context, container);
                });
            services.AddAuthorization(ConsoleAuth.AddPolicies);

            services.AddControllers(options => options.Filters.Add(new Filters.AdminAuditFilter(container)));

            services.AddSimpleInjector(container, options => {
                options.AddAspNetCore()
                    .AddControllerActivation();
            });

            services.Configure<OWSShared.Options.ManagementOptions>(Configuration.GetSection(OWSShared.Options.ManagementOptions.SectionName));
            services.Configure<OWSShared.Options.APIPathOptions>(Configuration.GetSection(OWSShared.Options.APIPathOptions.SectionName));

            var owsManagementOptions = new OWSShared.Options.ManagementOptions();
            Configuration.GetSection(OWSShared.Options.ManagementOptions.SectionName).Bind(owsManagementOptions);
            OWSData.Repositories.Implementations.Postgres.ConsoleQueriesRepository.LargeJuriChange = owsManagementOptions.LargeJuriChange;

            var apiPathOptions = new OWSShared.Options.APIPathOptions();
            Configuration.GetSection(OWSShared.Options.APIPathOptions.SectionName).Bind(apiPathOptions);

            services.AddSpaStaticFiles(options => options.RootPath = "wwwroot/dist");

            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Samsara Management API", Version = "v1" });

                var filePath = Path.Combine(System.AppContext.BaseDirectory, "OWSManagement.xml");
                c.IncludeXmlComments(filePath);
            });

            services.AddHttpClient("SteamOpenId", c => c.Timeout = TimeSpan.FromSeconds(10));
            services.AddHttpClient("Health", c => c.Timeout = TimeSpan.FromSeconds(5));
            services.AddHttpClient("OWSInstanceManagement", c =>
            {
                c.BaseAddress = new Uri(apiPathOptions.InternalInstanceManagementApiURL ?? "http://owsinstancemanagement/");
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                c.DefaultRequestHeaders.Add("User-Agent", "OWSManagement");
            });

            services.Configure<OWSShared.Options.StorageOptions>(Configuration.GetSection(OWSShared.Options.StorageOptions.SectionName));

            InitializeContainer(services, owsManagementOptions);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSimpleInjector(container);

            app.UseHealthChecks("/health");

            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("./v1/swagger.json", "Samsara Management API");
                });
            }

            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.Use(ConsoleAuth.RequireCsrfHeader);

            // Every API action needs Moderator unless it says otherwise. Not a FallbackPolicy: that would also lock the SPA's
            // own page, which has to load to show the sign-in button.
            app.UseEndpoints(endpoints => { endpoints.MapControllers().RequireAuthorization(ConsoleAuth.Moderator); });
            app.UseSpa(spa =>
            {
                if (!env.IsDevelopment())
                {
                    spa.Options.SourcePath = "wwwroot/dist";
                }
            });

            container.Verify();
        }

        private void InitializeContainer(IServiceCollection services, OWSShared.Options.ManagementOptions managementOptions)
        {
            var OWSStorageConfig = Configuration.GetSection("OWSStorageConfig");
            if (OWSStorageConfig.Exists())
            {
                string dbBackend = OWSStorageConfig.GetValue<string>("OWSDBBackend");

                switch (dbBackend)
                {
                    case "postgres":
                        container.Register<ICharactersRepository, OWSData.Repositories.Implementations.Postgres.CharactersRepository>(Lifestyle.Transient);
                        container.Register<IUsersRepository, OWSData.Repositories.Implementations.Postgres.UsersRepository>(Lifestyle.Transient);
                        container.Register<IInstanceManagementRepository, OWSData.Repositories.Implementations.Postgres.InstanceManagementRepository>(Lifestyle.Transient);
                        break;
                    default: // Default to MSSQL
                        container.Register<ICharactersRepository, OWSData.Repositories.Implementations.MSSQL.CharactersRepository>(Lifestyle.Transient);
                        container.Register<IUsersRepository, OWSData.Repositories.Implementations.MSSQL.UsersRepository>(Lifestyle.Transient);
                        container.Register<IInstanceManagementRepository, OWSData.Repositories.Implementations.MSSQL.InstanceManagementRepository>(Lifestyle.Transient);
                        break;
                }
                // The console's own tables, AuditLog and player shops are Postgres-only.
                container.Register<IAuditRepository, OWSData.Repositories.Implementations.Postgres.AuditRepository>(Lifestyle.Transient);
                container.Register<IConsoleQueriesRepository, OWSData.Repositories.Implementations.Postgres.ConsoleQueriesRepository>(Lifestyle.Transient);
                container.Register<ISanctionsRepository, OWSData.Repositories.Implementations.Postgres.SanctionsRepository>(Lifestyle.Transient);
                container.Register<IZoneOpsRepository, OWSData.Repositories.Implementations.Postgres.ZoneOpsRepository>(Lifestyle.Transient);
                container.Register<IChatLogRepository, OWSData.Repositories.Implementations.Postgres.ChatLogRepository>(Lifestyle.Transient);
                container.Register<IConsoleSettingsRepository, OWSData.Repositories.Implementations.Postgres.ConsoleSettingsRepository>(Lifestyle.Transient);
                container.Register<IPlayerShopsRepository, OWSData.Repositories.Implementations.Postgres.PlayerShopsRepository>(Lifestyle.Transient);
            }

            container.RegisterSingleton(() => new ConsoleCustomer(managementOptions.CustomerGUID, container.GetInstance<IConsoleQueriesRepository>()));
            container.Register<IHeaderCustomerGUID, ConsoleCustomerGUID>(Lifestyle.Singleton);
        }
    }
}
