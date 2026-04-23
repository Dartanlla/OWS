using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OWSData.Repositories.Interfaces;
using OWSInstanceManagement.Requests.Instance;
using OWSInstanceManagement.Requests.Zones;
using OWSShared.Implementations;
using OWSShared.Interfaces;
using OWSShared.Options;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace OWSInstanceManagementLambda;

public class Function
{
    public static IServiceProvider? services;
    private const string ConnectionStringEnvVar = "OWS_DB_CONNECTION_STRING";
    private const string LegacyConnectionStringEnvVar = "OWSDBConnectionString";
    private const string ConfigSectionConnectionStringEnvVar = "OWSStorageConfig__OWSDBConnectionString";

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public Function()
    {
        string owsDbConnectionString = GetRequiredConnectionString();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IInstanceManagementRepository, OWSData.Repositories.Implementations.Postgres.InstanceManagementRepository>();
        serviceCollection.AddSingleton<ICharactersRepository, OWSData.Repositories.Implementations.Postgres.CharactersRepository>();
        serviceCollection.AddSingleton<IUsersRepository, OWSData.Repositories.Implementations.Postgres.UsersRepository>();

        var storageOptions = new StorageOptions
        {
            OWSDBConnectionString = owsDbConnectionString
        };
        serviceCollection.AddSingleton<IOptions<StorageOptions>>(Options.Create(storageOptions));

        var rabbitMQOptions = new RabbitMQOptions
        {
            RabbitMQHostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
            RabbitMQPort = int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_PORT"), out var rmqPort) ? rmqPort : 5672,
            RabbitMQUserName = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? "guest",
            RabbitMQPassword = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest",
        };
        serviceCollection.AddSingleton<IOptions<RabbitMQOptions>>(Options.Create(rabbitMQOptions));
        serviceCollection.AddSingleton<IInstanceLauncherMessaging, RabbitMQInstanceLauncherMessaging>();

        services = serviceCollection.BuildServiceProvider();
    }

    private static string GetRequiredConnectionString()
    {
        string? connectionString =
            Environment.GetEnvironmentVariable(ConnectionStringEnvVar)
            ?? Environment.GetEnvironmentVariable(LegacyConnectionStringEnvVar)
            ?? Environment.GetEnvironmentVariable(ConfigSectionConnectionStringEnvVar);

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException(
            $"No database connection string was found. Set one of these AWS Lambda environment variables: " +
            $"{ConnectionStringEnvVar}, {LegacyConnectionStringEnvVar}, or {ConfigSectionConnectionStringEnvVar}.");
    }

    /// <summary>
    /// A single function that handles all methods for the OWSInstanceManagement API. Deployed as a single Lambda function.
    /// </summary>
    /// <param name="proxyRequest">The proxy request from the API Gateway.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest proxyRequest, ILambdaContext context)
    {
        string path = proxyRequest.Path ?? string.Empty;
        string action = path.Split('/').LastOrDefault(s => !string.IsNullOrEmpty(s)) ?? string.Empty;

        string? customerGuidStr = null;
        proxyRequest.Headers?.TryGetValue("X-CustomerGUID", out customerGuidStr);
        if (string.IsNullOrEmpty(customerGuidStr))
            proxyRequest.Headers?.TryGetValue("x-customerguid", out customerGuidStr);

        var customerGuid = new HeaderCustomerGUID
        {
            CustomerGUID = Guid.TryParse(customerGuidStr, out var g) ? g : Guid.Empty
        };

        var instanceManagementRepository = services!.GetRequiredService<IInstanceManagementRepository>();
        var charactersRepository = services!.GetRequiredService<ICharactersRepository>();
        var instanceLauncherMessaging = services!.GetRequiredService<IInstanceLauncherMessaging>();

        string body = proxyRequest.Body ?? "{}";
        object? result;

        switch (action)
        {
            // --- InstanceController ---
            case "SetZoneInstanceStatus":
            {
                var request = JsonSerializer.Deserialize<SetZoneInstanceStatusRequest>(body, _jsonOptions)!;
                request.SetData(instanceManagementRepository, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "ShutDownServerInstance":
            {
                var request = JsonSerializer.Deserialize<ShutDownServerInstanceRequest>(body, _jsonOptions)!;
                request.SetData(instanceLauncherMessaging, instanceManagementRepository, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "RegisterLauncher":
            {
                var request = JsonSerializer.Deserialize<RegisterInstanceLauncherRequest>(body, _jsonOptions)!;
                request.SetData(instanceManagementRepository, customerGuid);
                result = await request.Handle();
                break;
            }
            case "SpinUpServerInstance":
            {
                var request = JsonSerializer.Deserialize<SpinUpServerInstanceRequest>(body, _jsonOptions)!;
                request.SetData(instanceLauncherMessaging, charactersRepository, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "StartInstanceLauncher":
            {
                string? launcherGuid = null;
                proxyRequest.Headers?.TryGetValue("X-LauncherGUID", out launcherGuid);
                if (string.IsNullOrEmpty(launcherGuid))
                    proxyRequest.Headers?.TryGetValue("x-launcherguid", out launcherGuid);

                var request = new StartInstanceLauncherRequest();
                request.SetData(instanceManagementRepository, launcherGuid ?? string.Empty, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "ShutDownInstanceLauncher":
            {
                var request = JsonSerializer.Deserialize<ShutDownInstanceLauncherRequest>(body, _jsonOptions)!;
                request.SetData(instanceManagementRepository, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "GetServerToConnectTo":
            {
                var request = JsonSerializer.Deserialize<GetServerToConnectToRequest>(body, _jsonOptions)!;
                request.SetData(charactersRepository, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "GetZoneInstance":
            {
                int zoneInstanceId = JsonSerializer.Deserialize<int>(body, _jsonOptions);
                var request = new GetZoneInstanceRequest(zoneInstanceId, instanceManagementRepository, customerGuid);
                result = await request.Handle();
                break;
            }
            case "GetServerInstanceFromPort":
            {
                string remoteIp = proxyRequest.RequestContext?.Identity?.SourceIp
                    ?? (proxyRequest.Headers?.TryGetValue("X-Forwarded-For", out var fwd) == true ? fwd!.Split(',')[0].Trim() : "127.0.0.1");

                var request = JsonSerializer.Deserialize<GetServerInstanceFromPortRequest>(body, _jsonOptions)!;
                request.SetData(instanceManagementRepository, customerGuid, remoteIp);
                result = await request.Handle();
                break;
            }
            case "GetZoneInstancesForWorldServer":
            {
                var request = JsonSerializer.Deserialize<GetZoneInstancesForWorldServerRequest>(body, _jsonOptions)!;
                request.SetData(instanceManagementRepository, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "UpdateNumberOfPlayers":
            {
                var request = JsonSerializer.Deserialize<UpdateNumberOfPlayersRequest>(body, _jsonOptions)!;
                request.SetData(instanceManagementRepository, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "GetZoneInstancesForZone":
            {
                var request = JsonSerializer.Deserialize<GetZoneInstancesForZoneRequest>(body, _jsonOptions)!;
                request.SetData(instanceManagementRepository, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "GetCurrentWorldTime":
            {
                var request = new GetCurrentWorldTimeRequest();
                request.SetData(instanceManagementRepository, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            // --- ZonesController ---
            case "AddZone":
            {
                var request = JsonSerializer.Deserialize<AddZoneRequest>(body, _jsonOptions)!;
                request.SetData(instanceManagementRepository, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            // --- SystemController ---
            case "Status":
            {
                result = true;
                break;
            }
            default:
                return new APIGatewayProxyResponse
                {
                    StatusCode = 404,
                    Body = JsonSerializer.Serialize(new { ErrorMessage = $"Unknown action: {action}" }),
                    Headers = BuildResponseHeaders()
                };
        }

        return new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(result),
            Headers = BuildResponseHeaders()
        };
    }

    private static object? ExtractActionResultValue(IActionResult actionResult) =>
        actionResult is OkObjectResult ok ? ok.Value : null;

    private static Dictionary<string, string> BuildResponseHeaders() => new()
    {
        { "Access-Control-Allow-Headers", "Content-Type, x-api-key" },
        { "Access-Control-Allow-Methods", "OPTIONS,GET,POST" },
        { "Access-Control-Allow-Origin", "*" },
        { "Content-Type", "application/json" },
    };
}
