using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using OWSChat.Service;
using OWSShared.Implementations;
using ProtoBuf.Grpc.Server;
using System.IO;

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

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddCodeFirstGrpc();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Open World Server Chat Authentication API", Version = "v1" });

    c.AddSecurityDefinition("X-CustomerGUID", new OpenApiSecurityScheme
    {
        Description = "Authorization header using the X-CustomerGUID scheme",
        Name = "X-CustomerGUID",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "X-CustomerGUID"
    });

    c.OperationFilter<SwaggerSecurityRequirementsDocumentFilter>();

    var filePath = Path.Combine(System.AppContext.BaseDirectory, "OWSChat.xml");
    c.IncludeXmlComments(filePath);
});

var app = builder.Build();

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
    c.SwaggerEndpoint("./v1/swagger.json", "Chat Authentication API");
});

// Configure the HTTP request pipeline.
app.MapGrpcService<ChatService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
