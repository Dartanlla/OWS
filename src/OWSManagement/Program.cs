using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using OWSData.Repositories.Interfaces;
using OWSManagement.Components;
using OWSManagement.Services;
using Radzen;
var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHttpClient();
builder.Services.AddRadzenComponents();

StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration); //Add this

var Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

builder.Services.Configure<OWSShared.Options.StorageOptions>(Configuration.GetSection(OWSShared.Options.StorageOptions.SectionName));

var OWSStorageConfig = Configuration.GetSection("OWSStorageConfig");
if (OWSStorageConfig.Exists())
{
    string dbBackend = OWSStorageConfig.GetValue<string>("OWSDBBackend");

    switch (dbBackend)
    {
        case "postgres":
            builder.Services.AddTransient<ICharactersRepository, OWSData.Repositories.Implementations.Postgres.CharactersRepository>();
            builder.Services.AddTransient<IUsersRepository, OWSData.Repositories.Implementations.Postgres.UsersRepository>();
            builder.Services.AddTransient<IInstanceManagementRepository, OWSData.Repositories.Implementations.Postgres.InstanceManagementRepository>();
            break;
        case "mysql":
            builder.Services.AddTransient<ICharactersRepository, OWSData.Repositories.Implementations.MySQL.CharactersRepository>();
            builder.Services.AddTransient<IUsersRepository, OWSData.Repositories.Implementations.MySQL.UsersRepository>();
            builder.Services.AddTransient<IInstanceManagementRepository, OWSData.Repositories.Implementations.MySQL.InstanceManagementRepository>();
            break;
        default: // Default to MSSQL
            builder.Services.AddTransient<ICharactersRepository, OWSData.Repositories.Implementations.MSSQL.CharactersRepository>();
            builder.Services.AddTransient<IUsersRepository, OWSData.Repositories.Implementations.MSSQL.UsersRepository>();
            builder.Services.AddTransient<IInstanceManagementRepository, OWSData.Repositories.Implementations.Postgres.InstanceManagementRepository>();
            break;
    }
}

builder.Services.AddSingleton<IUsersService, UsersService>();
builder.Services.AddSingleton<ICharactersService, CharactersService>();
builder.Services.AddSingleton<IInstanceManagementService, InstanceManagementService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
