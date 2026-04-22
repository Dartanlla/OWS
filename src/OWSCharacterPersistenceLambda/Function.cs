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
    /// A single function that handles all methods for the OWSCharacterPersistence API. Deployed as a single Lambda function.
    /// </summary>
    /// <param name="proxyRequest">The proxy request from the API Gateway.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest proxyRequest, ILambdaContext context)
    {
        return input.ToUpper();
    }
}
