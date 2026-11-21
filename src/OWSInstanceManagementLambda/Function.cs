using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Runtime;
using Amazon.SQS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OWSData.Repositories.Interfaces;
using OWSInstanceManagement.Requests.Instance;
using OWSInstanceManagement.Requests.Zones;
using OWSInstanceManagement.Services;
using OWSData.Repositories.Implementations.ValKey;
using OWSShared.Implementations;
using OWSShared.Interfaces;
using OWSShared.Options;
using System.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace OWSInstanceManagementLambda;

public class Function
{
    public static IServiceProvider? services;
    private const string ConnectionStringEnvVar = "OWS_DB_CONNECTION_STRING";
    private const string LegacyConnectionStringEnvVar = "OWSDBConnectionString";
    private const string ConfigSectionConnectionStringEnvVar = "OWSStorageConfig__OWSDBConnectionString";
    private static readonly Lazy<ITicketRepository?> TicketStore = new(() =>
        Environment.GetEnvironmentVariable("OWSTicketConfig__ConnectionString") is { Length: > 0 } value
            ? new TicketRepository(value) : null);

    private static async Task<APIGatewayHttpApiV2ProxyResponse> RedeemTicketAsync(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        APIGatewayHttpApiV2ProxyResponse Reply(int status, object payload) => new()
        {
            StatusCode = status,
            Body = JsonSerializer.Serialize(payload),
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json", ["Cache-Control"] = "no-store" }
        };

        if (!string.Equals(request.RequestContext?.Http?.Method, "POST", StringComparison.OrdinalIgnoreCase))
            return Reply(405, new { ErrorMessage = "POST is required." });

        RedeemExternalConnectionTicketRequest? ticketRequest;
        try
        {
            var body = request.Body ?? "{}";
            if (request.IsBase64Encoded)
                body = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(body));
            ticketRequest = JsonSerializer.Deserialize<RedeemExternalConnectionTicketRequest>(body, _jsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            return Reply(400, new { ErrorMessage = "Invalid JSON request body." });
        }

        if (ticketRequest == null || !ExternalConnectionTicketRedemption.IsValid(ticketRequest))
            return Reply(400, new { ErrorMessage = "Ticket, a positive ServerId, and ClaimantId when claiming are required." });

        try
        {
            var credential = request.Headers?.FirstOrDefault(header =>
                string.Equals(header.Key, ServerCredentials.CredentialHeader, StringComparison.OrdinalIgnoreCase)).Value;
            var hash = ServerCredentials.Hash(credential);
            var instances = services!.GetRequiredService<IInstanceManagementRepository>();
            var tenant = hash == null ? null : await instances.AuthenticateActiveEdgeServer(ticketRequest.ServerId, hash);
            if (!tenant.HasValue)
                return Reply(401, new { ErrorMessage = "Invalid Edge server credential." });
            if (TicketStore.Value is not ITicketRepository repository)
                return Reply(503, new { ErrorMessage = "The connection ticket store is not configured." });

            var ticket = await ExternalConnectionTicketRedemption.ExecuteAsync(repository, ticketRequest, tenant.Value);
            return ticket == null
                ? Reply(404, new { ErrorMessage = "The ticket is unavailable." })
                : Reply(200, ExternalConnectionTicketRedemption.ToResponse(ticket));
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Connection ticket redemption failed: {ex.GetType().Name}");
            return Reply(503, new { ErrorMessage = "Ticket redemption is unavailable." });
        }
    }

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
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

        /*
        var rabbitMQOptions = new RabbitMQOptions
        {
            RabbitMQHostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
            RabbitMQPort = int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_PORT"), out var rmqPort) ? rmqPort : 5672,
            RabbitMQUserName = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? "guest",
            RabbitMQPassword = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest",
        };
        serviceCollection.AddSingleton<IOptions<RabbitMQOptions>>(Options.Create(rabbitMQOptions));
        serviceCollection.AddSingleton<IInstanceLauncherMessaging, RabbitMQInstanceLauncherMessaging>();
        */

        var sqsOptions = new SQSOptions
        {
            AWSRegion = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1",
            QueueName = Environment.GetEnvironmentVariable("QUEUE_NAME") ?? "ows-instance-launcher-messaging"
        };

        serviceCollection.AddSingleton<IOptions<SQSOptions>>(Options.Create(sqsOptions));
        serviceCollection.AddAWSService<IAmazonSQS>();
        serviceCollection.AddSingleton<IInstanceLauncherMessaging, SQSInstanceLauncherMessaging>();

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
    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest proxyRequest, ILambdaContext context)
    {
        string path = proxyRequest.RawPath ?? string.Empty;
        if (string.Equals(path.TrimEnd('/'), "/api/Instance/RedeemExternalConnectionTicket", StringComparison.OrdinalIgnoreCase))
            return await RedeemTicketAsync(proxyRequest, context);

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
        string? Header(string name) => proxyRequest.Headers?.FirstOrDefault(header =>
            string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase)).Value;

        var isEdgeOperation = EdgeServerOperations.TryParseOperation(action, out var edgeOperation);
        var operation = isEdgeOperation || action is "CheckReadyAssignment" or "GetReadyMapInstances" or "AddMapInstance";
        if (operation)
        {
            APIGatewayHttpApiV2ProxyResponse Reply(int status, object payload) => new()
            {
                StatusCode = status,
                Body = JsonSerializer.Serialize(payload),
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json", ["Cache-Control"] = "no-store" }
            };

            if (!string.Equals(path.TrimEnd('/'), "/api/Instance/" + action, StringComparison.OrdinalIgnoreCase))
                return Reply(404, new { ErrorMessage = "Unknown route." });
            if (!string.Equals(proxyRequest.RequestContext?.Http?.Method, "POST", StringComparison.OrdinalIgnoreCase))
                return Reply(405, new { ErrorMessage = "POST is required." });
            try
            {
                var bodyText = proxyRequest.Body ?? "{}";
                if (proxyRequest.IsBase64Encoded)
                    bodyText = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(bodyText));
                if (isEdgeOperation)
                {
                    var edgeRequest = JsonSerializer.Deserialize<EdgeServerRequest>(bodyText, _jsonOptions);
                    var (edgeStatus, edgePayload) = await EdgeServerOperations.ExecuteAsync(edgeOperation, edgeRequest,
                        Header(ServerCredentials.LauncherHeader), Header(ServerCredentials.CredentialHeader), instanceManagementRepository);
                    return Reply(edgeStatus, edgePayload);
                }

                var input = JsonSerializer.Deserialize<MapInstanceOperation>(bodyText, _jsonOptions);
                var (status, payload) = await MapInstanceOperations.ExecuteAsync(action, input,
                    Header(ServerCredentials.LauncherHeader), Header(ServerCredentials.AssignmentHeader), instanceManagementRepository);
                return Reply(status, payload);
            }
            catch (Exception ex) when (ex is JsonException or FormatException)
            {
                return Reply(400, new { ErrorMessage = "Invalid JSON request body." });
            }
        }

        var charactersRepository = services!.GetRequiredService<ICharactersRepository>();
        var instanceLauncherMessaging = services!.GetRequiredService<IInstanceLauncherMessaging>();

        string body = proxyRequest.Body ?? "{}";
        object? result;

        switch (action)
        {
            // --- InstanceController ---
            case "SetZoneInstanceStatus":
            {
                SetZoneInstanceStatusRequest? request;
                try
                {
                    request = JsonSerializer.Deserialize<SetZoneInstanceStatusRequest>(body, _jsonOptions);
                }
                catch (JsonException)
                {
                    return InvalidZoneStatusRequest();
                }
                if (request?.request == null)
                    return InvalidZoneStatusRequest();
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
                string remoteIp = proxyRequest.RequestContext?.Http?.SourceIp
                    ?? (proxyRequest.Headers?.TryGetValue("x-forwarded-for", out var fwd) == true ? fwd!.Split(',')[0].Trim() : "127.0.0.1");

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
                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = 404,
                    Body = JsonSerializer.Serialize(new { ErrorMessage = $"Unknown action: {action}" }),
                    Headers = BuildResponseHeaders()
                };
        }

        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(result),
            Headers = BuildResponseHeaders()
        };
    }

    private static object? ExtractActionResultValue(IActionResult actionResult) =>
        actionResult is OkObjectResult ok ? ok.Value : null;

    private static APIGatewayHttpApiV2ProxyResponse InvalidZoneStatusRequest() => new()
    {
        StatusCode = 400,
        Body = JsonSerializer.Serialize(new { ErrorMessage = "A zone instance status request is required." }),
        Headers = BuildResponseHeaders()
    };

    private static Dictionary<string, string> BuildResponseHeaders() => new()
    {
        { "Access-Control-Allow-Headers", "Content-Type, x-api-key" },
        { "Access-Control-Allow-Methods", "OPTIONS,GET,POST" },
        { "Access-Control-Allow-Origin", "*" },
        { "Content-Type", "application/json" },
    };
}
