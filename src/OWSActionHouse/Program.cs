using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using SimpleInjector;
using OWSShared.Implementations;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSShared.Middleware;
using System.IO;
using System;
using Microsoft.Extensions.Hosting;
using System.Configuration;
using ProtoBuf.Grpc.Configuration;

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

builder.Services.AddSimpleInjector(container, options =>
{
    options.AddAspNetCore()
        .AddControllerActivation()
        .AddViewComponentActivation();

});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Action House Authentication API", Version = "v1" });

    c.AddSecurityDefinition("X-CustomerGUID", new OpenApiSecurityScheme
    {
        Description = "Authorization header using the X-CustomerGUID scheme",
        Name = "X-CustomerGUID",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "X-CustomerGUID"
    });

    c.OperationFilter<SwaggerSecurityRequirementsDocumentFilter>();

    var filePath = Path.Combine(System.AppContext.BaseDirectory, "OWSActionHouse.xml");
    c.IncludeXmlComments(filePath);
});

var apiPathOptions = new OWSShared.Options.APIPathOptions();
builder.Configuration.GetSection(OWSShared.Options.APIPathOptions.SectionName).Bind(apiPathOptions);

builder.Services.Configure<OWSShared.Options.StorageOptions>(builder.Configuration.GetSection(OWSShared.Options.StorageOptions.SectionName));
builder.Services.Configure<OWSShared.Options.APIPathOptions>(builder.Configuration.GetSection(OWSShared.Options.APIPathOptions.SectionName));
builder.Services.Configure<OWSShared.Options.RabbitMQOptions>(builder.Configuration.GetSection(OWSShared.Options.RabbitMQOptions.SectionName));

var OWSStorageConfig = builder.Configuration.GetSection("OWSStorageConfig");
if (OWSStorageConfig.Exists())
{
    string dbBackend = OWSStorageConfig.GetValue<string>("OWSDBBackend");

    switch (dbBackend)
    {
        default: // Default to MSSQL
            container.Register<IActionHouseRepository, OWSData.Repositories.Implementations.MSSQL.ActionHouseRepository>(Lifestyle.Transient);
            //container.Register<IUsersRepository, OWSData.Repositories.Implementations.MSSQL.UsersRepository>(Lifestyle.Transient);
            break;
    }
}

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
    c.SwaggerEndpoint("./v1/swagger.json", "Action House Authentication API");
});

app.MapGet("/", () => "Welcome to Action House");


container.Verify();

app.Run();


