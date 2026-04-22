using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OWSCharacterPersistence.Requests.Abilities;
using OWSCharacterPersistence.Requests.Characters;
using OWSCharacterPersistence.Requests.Statuses;
using OWSData.Repositories.Interfaces;
using OWSShared.Implementations;
using OWSShared.Options;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace OWSCharacterPersistenceLambda;

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
        serviceCollection.AddSingleton<ICharactersRepository, OWSData.Repositories.Implementations.Postgres.CharactersRepository>();
        serviceCollection.AddSingleton<IUsersRepository, OWSData.Repositories.Implementations.Postgres.UsersRepository>();

        var storageOptions = new StorageOptions
        {
            OWSDBConnectionString = owsDbConnectionString
        };
        serviceCollection.AddSingleton<IOptions<StorageOptions>>(Options.Create(storageOptions));

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
    /// A single function that handles all methods for the OWSCharacterPersistence API. Deployed as a single Lambda function.
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

        var charactersRepository = services!.GetRequiredService<ICharactersRepository>();

        string body = proxyRequest.Body ?? "{}";
        object? result;

        switch (action)
        {
            // --- AbilitiesController ---
            case "AddAbilityToCharacter":
            {
                var request = JsonSerializer.Deserialize<AddAbilityToCharacterRequest>(body, _jsonOptions)!;
                request.SetData(charactersRepository, customerGuid);
                result = await request.Handle();
                break;
            }
            case "GetCharacterAbilities":
            {
                var request = JsonSerializer.Deserialize<GetCharacterAbilitiesRequest>(body, _jsonOptions)!;
                request.SetData(charactersRepository, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "GetAbilities":
            {
                var request = new GetAbilitiesRequest();
                request.SetData(charactersRepository, customerGuid);
                result = await request.Handle();
                break;
            }
            case "GetAbilityBars":
            {
                var request = JsonSerializer.Deserialize<GetAbilityBarsRequest>(body, _jsonOptions)!;
                request.SetData(charactersRepository, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "GetAbilityBarsAndAbilities":
            {
                var request = JsonSerializer.Deserialize<GetAbilityBarsAndAbilitiesRequest>(body, _jsonOptions)!;
                request.SetData(charactersRepository, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "RemoveAbilityFromCharacter":
            {
                var request = JsonSerializer.Deserialize<RemoveAbilityFromCharacterRequest>(body, _jsonOptions)!;
                request.SetData(charactersRepository, customerGuid);
                result = await request.Handle();
                break;
            }
            case "UpdateAbilityOnCharacter":
            {
                var request = JsonSerializer.Deserialize<UpdateAbilityOnCharacterRequest>(body, _jsonOptions)!;
                request.SetData(charactersRepository, customerGuid);
                result = await request.Handle();
                break;
            }
            // --- CharactersController ---
            case "AddOrUpdateCustomData":
            {
                var request = JsonSerializer.Deserialize<AddOrUpdateCustomDataRequest>(body, _jsonOptions)!;
                request.SetData(charactersRepository, customerGuid);
                await request.Handle();
                result = null;
                break;
            }
            case "GetByName":
            {
                var request = JsonSerializer.Deserialize<GetByNameRequest>(body, _jsonOptions)!;
                request.SetData(charactersRepository, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "GetCustomData":
            {
                var request = JsonSerializer.Deserialize<GetCustomDataRequest>(body, _jsonOptions)!;
                request.SetData(charactersRepository, customerGuid);
                result = await request.Handle();
                break;
            }
            case "UpdateAllPlayerPositions":
            {
                var request = JsonSerializer.Deserialize<UpdateAllPlayerPositionsRequest>(body, _jsonOptions)!;
                request.SetData(charactersRepository, customerGuid);
                result = await request.Handle();
                break;
            }
            case "UpdateCharacterStats":
            {
                var request = JsonSerializer.Deserialize<UpdateCharacterStatsRequest>(body, _jsonOptions)!;
                request.SetData(charactersRepository, customerGuid);
                result = await request.Handle();
                break;
            }
            case "PlayerLogout":
            {
                var request = JsonSerializer.Deserialize<PlayerLogoutRequest>(body, _jsonOptions)!;
                request.SetData(charactersRepository, customerGuid);
                result = await request.Handle();
                break;
            }
            // --- StatusController ---
            case "GetCharacterStatuses":
            {
                var request = JsonSerializer.Deserialize<GetCharacterStatusesRequest>(body, _jsonOptions)!;
                request.SetData(charactersRepository, customerGuid);
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
