using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using SimpleInjector.Integration.AspNetCore.Mvc;
using OWSShared.Implementations;
using System.Configuration;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSShared.Middleware;
using OWSGuild.Service;
using ProtoBuf.Grpc.Server;
using System.IO;
using System;
using Microsoft.Extensions.Hosting;
using Grpc.AspNetCore.Server;
using OWSGuild;

SimpleInjector.Container container = new SimpleInjector.Container();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo("./temp/DataProtection-Keys"));

builder.Services.AddMemoryCache();

builder.Services.AddHttpContextAccessor();

builder.Services.AddMvcCore(config =>
{
    config.EnableEndpointRouting = false;
})
.AddViews()
.AddApiExplorer();

builder.Services.AddCodeFirstGrpc();
builder.Services.AddSingleton(typeof(IGrpcServiceActivator<>),
            typeof(GrpcSimpleInjectorActivator<>));

builder.Services.AddSimpleInjector(container, options =>
{
    options.AddAspNetCore()
        .AddControllerActivation()
        .AddViewComponentActivation();

});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Guild Authentication API", Version = "v1" });

    c.AddSecurityDefinition("X-CustomerGUID", new OpenApiSecurityScheme
    {
        Description = "Authorization header using the X-CustomerGUID scheme",
        Name = "X-CustomerGUID",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "X-CustomerGUID"
    });

    c.OperationFilter<SwaggerSecurityRequirementsDocumentFilter>();

    var filePath = Path.Combine(System.AppContext.BaseDirectory, "OWSGuild.xml");
    c.IncludeXmlComments(filePath);
});


container.Register<GuildService>();

var apiPathOptions = new OWSShared.Options.APIPathOptions();
builder.Configuration.GetSection(OWSShared.Options.APIPathOptions.SectionName).Bind(apiPathOptions);

builder.Services.Configure<OWSShared.Options.PublicAPIOptions>(builder.Configuration.GetSection(OWSShared.Options.PublicAPIOptions.SectionName));
builder.Services.Configure<OWSShared.Options.APIPathOptions>(builder.Configuration.GetSection(OWSShared.Options.APIPathOptions.SectionName));
builder.Services.Configure<OWSData.Models.StorageOptions>(builder.Configuration.GetSection(OWSData.Models.StorageOptions.SectionName));

// Register And Validate External Login Provider Options
// services.ConfigureAndValidate<EpicOnlineServicesOptions>(ExternalLoginProviderOptions.EpicOnlineServices, Configuration.GetSection($"{ExternalLoginProviderOptions.SectionName}:{ExternalLoginProviderOptions.EpicOnlineServices}"));

var OWSStorageConfig = builder.Configuration.GetSection("OWSStorageConfig");
if (OWSStorageConfig.Exists())
{
    string dbBackend = OWSStorageConfig.GetValue<string>("OWSDBBackend");

    switch (dbBackend)
    {
        default: // Default to MSSQL
            container.Register<ICharactersRepository, OWSData.Repositories.Implementations.MSSQL.CharactersRepository>(Lifestyle.Transient);
            container.Register<IUsersRepository, OWSData.Repositories.Implementations.MSSQL.UsersRepository>(Lifestyle.Transient);
            break;
    }
}

container.Register<IPublicAPIInputValidation, DefaultPublicAPIInputValidation>(Lifestyle.Singleton);
container.Register<ICustomCharacterDataSelector, DefaultCustomCharacterDataSelector>(Lifestyle.Singleton);
container.Register<IGetReadOnlyPublicCharacterData, DefaultGetReadOnlyPublicCharacterData>(Lifestyle.Singleton);
container.Register<IHeaderCustomerGUID, HeaderCustomerGUID>(Lifestyle.Scoped);

var app = builder.Build();

container.RegisterInstance<IServiceProvider>(app.Services);

app.Services.UseSimpleInjector(container);

app.UseMiddleware<StoreCustomerGUIDMiddleware>(container);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
}

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
    c.SwaggerEndpoint("./v1/swagger.json", "Guild Authentication API");
});

app.MapGrpcService<GuildService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");


container.Verify();

app.Run();