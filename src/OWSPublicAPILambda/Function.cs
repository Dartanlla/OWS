using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OWSData.Repositories.Interfaces;
using OWSExternalLoginProviders.Extensions;
using OWSExternalLoginProviders.Interfaces;
using OWSPublicAPI;
using OWSPublicAPI.DTOs;
using OWSPublicAPI.Requests.Characters;
using OWSPublicAPI.Requests.Users;
using OWSShared.Implementations;
using OWSShared.Interfaces;
using OWSShared.Options;
using SimpleInjector;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace OWSPublicAPILambda;

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
        string? instanceManagementApiUrl = Environment.GetEnvironmentVariable("INSTANCE_MANAGEMENT_API_URL") ?? "http://localhost:5000/";
        string owsDbConnectionString = GetRequiredConnectionString();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ICharactersRepository, OWSData.Repositories.Implementations.Postgres.CharactersRepository>();
        serviceCollection.AddSingleton<IUsersRepository, OWSData.Repositories.Implementations.Postgres.UsersRepository>();
        serviceCollection.AddSingleton<IPublicAPIInputValidation, DefaultPublicAPIInputValidation>();
        serviceCollection.AddSingleton<ICustomCharacterDataSelector, DefaultCustomCharacterDataSelector>();
        serviceCollection.AddSingleton<ICustomDataSelector, DefaultCustomDataSelector>();
        serviceCollection.AddSingleton<IGetReadOnlyPublicCharacterData, DefaultGetReadOnlyPublicCharacterData>();

        serviceCollection.AddHttpClient("OWSInstanceManagement", c =>
        {
            c.BaseAddress = new Uri(instanceManagementApiUrl);
            c.DefaultRequestHeaders.Add("Accept", "application/json");
            c.DefaultRequestHeaders.Add("User-Agent", "OWSPublicAPILambda");
        });

        var publicAPIOptions = new PublicAPIOptions
        {
            SecondsToWaitForServerSpinUp = int.TryParse(Environment.GetEnvironmentVariable("SECONDS_TO_WAIT_FOR_SERVER_SPINUP"), out var s1) ? s1 : 30,
            SecondsToWaitInBetweenSpinUpPolling = int.TryParse(Environment.GetEnvironmentVariable("SECONDS_BETWEEN_SPINUP_POLLING"), out var s2) ? s2 : 5,
            SecondsToWaitBeforeFirstPollForSpinUp = int.TryParse(Environment.GetEnvironmentVariable("SECONDS_BEFORE_FIRST_SPINUP_POLL"), out var s3) ? s3 : 5,
        };
        serviceCollection.AddSingleton<IOptions<PublicAPIOptions>>(Options.Create(publicAPIOptions));

        var storageOptions = new StorageOptions
        {
            OWSDBConnectionString = owsDbConnectionString
        };
        serviceCollection.AddSingleton<IOptions<StorageOptions>>(Options.Create(storageOptions));

        serviceCollection.AddSingleton<IExternalLoginProviderFactory>(new ExternalLoginProviderFactory(new Container()));

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
    /// A single function that handles all methods for the OWSPublicAPI. Deployed as a single Lambda function.
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

        var usersRepository = services!.GetRequiredService<IUsersRepository>();
        var charactersRepository = services!.GetRequiredService<ICharactersRepository>();
        var publicAPIInputValidation = services!.GetRequiredService<IPublicAPIInputValidation>();
        var externalLoginProviderFactory = services!.GetRequiredService<IExternalLoginProviderFactory>();
        var owsGeneralConfig = services!.GetRequiredService<IOptions<PublicAPIOptions>>();
        var httpClientFactory = services!.GetRequiredService<IHttpClientFactory>();
        var customCharacterDataSelector = services!.GetRequiredService<ICustomCharacterDataSelector>();
        var customDataSelector = services!.GetRequiredService<ICustomDataSelector>();
        var getReadOnlyPublicCharacterData = services!.GetRequiredService<IGetReadOnlyPublicCharacterData>();

        string body = proxyRequest.Body ?? "{}";
        object? result;

        switch (action)
        {
            case "CreateCharacter":
            {
                var request = JsonSerializer.Deserialize<CreateCharacterRequest>(body, _jsonOptions)!;
                request.SetData(usersRepository, publicAPIInputValidation, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "CreateCharacterUsingDefaultCharacterValues":
            {
                var dto = JsonSerializer.Deserialize<CreateCharacterUsingDefaultCharacterValuesDTO>(body, _jsonOptions)!;
                var request = new CreateCharacterUsingDefaultCharacterValuesRequest(dto, usersRepository, charactersRepository, publicAPIInputValidation, customerGuid);
                result = await request.Handle();
                break;
            }
            case "GetAllCharacters":
            {
                var request = JsonSerializer.Deserialize<GetAllCharactersRequest>(body, _jsonOptions)!;
                request.SetData(usersRepository, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "GetPlayerGroupsCharacterIsIn":
            {
                var request = JsonSerializer.Deserialize<GetPlayerGroupsCharacterIsInRequest>(body, _jsonOptions)!;
                request.SetData(usersRepository, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "GetServerToConnectTo":
            {
                var request = JsonSerializer.Deserialize<GetServerToConnectToRequest>(body, _jsonOptions)!;
                request.SetData(owsGeneralConfig, usersRepository, charactersRepository, customerGuid, httpClientFactory);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "GetUserSession":
            {
                var request = new GetUserSessionRequest();
                string? userSessionGuidStr = null;
                proxyRequest.QueryStringParameters?.TryGetValue("UserSessionGUID", out userSessionGuidStr);
                if (Guid.TryParse(userSessionGuidStr, out var sessionGuid))
                    request.UserSessionGUID = sessionGuid;
                request.SetData(usersRepository, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "LoginAndCreateSession":
            {
                var request = JsonSerializer.Deserialize<LoginAndCreateSessionRequest>(body, _jsonOptions)!;
                request.SetData(usersRepository, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "ExternalLoginAndCreateSession":
            {
                var request = JsonSerializer.Deserialize<ExternalLoginAndCreateSessionRequest>(body, _jsonOptions)!;
                request.SetData(usersRepository, externalLoginProviderFactory, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "Logout":
            {
                var dto = JsonSerializer.Deserialize<LogoutDTO>(body, _jsonOptions)!;
                var request = new LogoutRequest(dto, usersRepository, customerGuid);
                result = await request.Handle();
                break;
            }
            case "UserSessionSetSelectedCharacter":
            {
                var request = JsonSerializer.Deserialize<UserSessionSetSelectedCharacterRequest>(body, _jsonOptions)!;
                request.SetData(usersRepository, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "SetSelectedCharacterAndGetUserSession":
            {
                var request = JsonSerializer.Deserialize<SetSelectedCharacterAndGetUserSessionRequest>(body, _jsonOptions)!;
                request.SetData(usersRepository, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "RegisterUser":
            {
                var dto = JsonSerializer.Deserialize<RegisterUserDTO>(body, _jsonOptions)!;
                var request = new RegisterUserRequest(dto, usersRepository, externalLoginProviderFactory, customerGuid);
                result = await request.Handle();
                break;
            }
            case "RemoveCharacter":
            {
                var request = JsonSerializer.Deserialize<RemoveCharacterRequest>(body, _jsonOptions)!;
                request.SetData(usersRepository, customerGuid);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "ByName":
            {
                var dto = JsonSerializer.Deserialize<GetByNameDTO>(body, _jsonOptions)!;
                var request = new GetByNameRequest(dto, usersRepository, charactersRepository, customerGuid, customCharacterDataSelector, getReadOnlyPublicCharacterData);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
            case "GetDefaultCustomData":
            {
                var dto = JsonSerializer.Deserialize<GetDefaultCustomrDataDTO>(body, _jsonOptions)!;
                var request = new GetDefaultCustomDataRequest(dto, usersRepository, charactersRepository, customerGuid, customDataSelector, getReadOnlyPublicCharacterData);
                result = ExtractActionResultValue(await request.Handle());
                break;
            }
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
