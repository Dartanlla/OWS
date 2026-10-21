using System;
using System.IO;
using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using SimpleInjector;
using SimpleInjector.Integration.ServiceCollection;
using SimpleInjector.Integration.AspNetCore;
using OWSChat;
using OWSChat.Service;
using OWSData.Repositories.Interfaces;
using OWSShared.Implementations;
using OWSShared.Interfaces;
using OWSShared.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services));

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("./temp/DataProtection-Keys"));
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddSingleton(typeof(IGrpcServiceActivator<>), typeof(GrpcSimpleInjectorActivator<>));
builder.Services.Configure<OWSShared.Options.StorageOptions>(builder.Configuration.GetSection(OWSShared.Options.StorageOptions.SectionName));
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Chat Auth API", Version = "v1" });
});
builder.Services.AddScoped<IHeaderCustomerGUID, HeaderCustomerGUID>();

var container = new Container();
builder.Services.AddSimpleInjector(container, options =>
{
    options.AddAspNetCore()
           .AddControllerActivation();
});

container.Register<ChatService>();
container.Register<IHeaderCustomerGUID, HeaderCustomerGUID>(Lifestyle.Scoped);
if (builder.Configuration.GetValue<string>("OWSStorageConfig:OWSDBBackend") == "postgres")
    container.Register<IUsersRepository, OWSData.Repositories.Implementations.Postgres.UsersRepository>(Lifestyle.Transient);
else
    container.Register<IUsersRepository, OWSData.Repositories.Implementations.MSSQL.UsersRepository>(Lifestyle.Transient);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(50051, lo => lo.Protocols = HttpProtocols.Http2);
});

builder.Services.AddScoped<StoreCustomerGUIDMiddleware>();

var app = builder.Build();

((IApplicationBuilder)app).UseSimpleInjector(container);

app.UseRouting();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Chat Auth API v1");
});
// gRPC requests carry their own identity; only REST routes require the header.
StoreCustomerGUIDMiddleware.ProtectedPathPrefixes = new[] { "/api" };
app.UseMiddleware<StoreCustomerGUIDMiddleware>();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGrpcService<ChatService>();
    endpoints.MapControllers();
    endpoints.MapGet("/", () => "gRPC server is running ‑ connect with UE client");
});

container.Verify();
app.Run();
