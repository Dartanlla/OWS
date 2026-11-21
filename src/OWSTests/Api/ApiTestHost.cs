using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OWSData.Repositories.Interfaces;
using OWSExternalLoginProviders.Interfaces;
using OWSPublicAPI.Services;
using OWSShared.Implementations;
using OWSShared.Interfaces;
using OWSShared.Middleware;
using SimpleInjector;
using Xunit;
using PersistenceLambda = OWSCharacterPersistenceLambda.Function;
using PublicLambda = OWSPublicAPILambda.Function;
using InstanceLambda = OWSInstanceManagementLambda.Function;

namespace OWSTests.Api;

// Lambda dispatchers and credential configuration use process-wide state.
[CollectionDefinition(Name, DisableParallelization = true)]
public class ApiTestCollection
{
    public const string Name = "OWS API contracts";
}

public enum ApiService
{
    Public, Persistence, Instance, GlobalData, Content, Management
}

internal sealed class ApiTestHost
{
    public static readonly Guid Tenant = Guid.Parse("ca5ad676-a8fd-4a65-b6df-37e9ad625e28");
    public static readonly Guid Session = Guid.Parse("69f84a14-fce3-48ef-9b22-f4c651c8af74");
    public static readonly Guid Owner = Guid.Parse("0e7f0b5e-996f-4f94-94a8-f1dc429112e2");
    public const string RemoteIp = "192.0.2.12";
    public const int PrivatePort = 9443;
    private static readonly ILambdaContext LambdaContext = Characters.RepositoryProxy.Create<ILambdaContext>(
        (method, _) => method.Name == "get_Logger" ? new LambdaLogger() : throw new InvalidOperationException(method.Name));
    private readonly IServiceCollection services = new ServiceCollection();

    public ApiTestHost()
    {
        services.AddOptions();
        services.AddSingleton(Serilog.Log.Logger);
        services.AddSingleton<IPublicAPIInputValidation, DefaultPublicAPIInputValidation>();
        services.AddSingleton<ICustomCharacterDataSelector, DefaultCustomCharacterDataSelector>();
        services.AddSingleton<ICustomDataSelector, DefaultCustomDataSelector>();
        services.AddSingleton<IGetReadOnlyPublicCharacterData, DefaultGetReadOnlyPublicCharacterData>();
        Add<IExternalLoginProviderFactory>();
        Add<IHttpClientFactory>();
        services.AddScoped<IHeaderCustomerGUID, HeaderCustomerGUID>();
        services.AddTransient<StoreCustomerGUIDMiddleware>();
        services.AddTransient<PublicApiTenant>();
        Add<IUsersRepository>((method, args) => method.Name == nameof(IUsersRepository.CustomerExists)
            ? Task.FromResult(Tenant.Equals(args[0]))
            : throw new InvalidOperationException($"Unexpected call to {method.Name}."));
        Add<ICharactersRepository>();
        Add<IInstanceManagementRepository>();
        Add<IContentRepository>();
        Add<IContentCacheRepository>();
        Add<IGlobalDataRepository>();
        Add<IInstanceLauncherMessaging>();
    }

    public void Add<T>(Func<MethodInfo, object[], object> call = null) where T : class =>
        services.AddSingleton(Characters.RepositoryProxy.Create<T>(call ?? ((method, _) =>
            throw new InvalidOperationException($"Unexpected call to {typeof(T).Name}.{method.Name}."))));

    public void Add<T>(T instance) where T : class => services.AddSingleton(instance);

    public static Assembly Controllers(ApiService service) => service switch
    {
        ApiService.Public => typeof(OWSPublicAPI.Controllers.UsersController).Assembly,
        ApiService.Persistence => typeof(OWSCharacterPersistence.Controllers.CharactersController).Assembly,
        ApiService.Instance => typeof(OWSInstanceManagement.Controllers.InstanceController).Assembly,
        ApiService.GlobalData => typeof(OWSGlobalData.Controllers.GlobalDataController).Assembly,
        ApiService.Content => typeof(OWSContent.Controllers.ContentController).Assembly,
        ApiService.Management => typeof(OWSManagement.Controllers.UsersController).Assembly,
        _ => throw new ArgumentOutOfRangeException(nameof(service))
    };

    public ApiServer Start(ApiService service)
    {
        bool legacyMvc = service is ApiService.Persistence or ApiService.Instance or ApiService.GlobalData;
        var host = new HostBuilder().ConfigureWebHost(builder => builder.UseTestServer()
            .ConfigureServices(web =>
            {
                foreach (var registration in services)
                    web.Add(registration);

                web.AddControllers(options => options.EnableEndpointRouting = !legacyMvc).ConfigureApplicationPartManager(parts =>
                {
                    parts.ApplicationParts.Clear();
                    parts.ApplicationParts.Add(new Microsoft.AspNetCore.Mvc.ApplicationParts.AssemblyPart(Controllers(service)));
                });
                web.AddScoped(provider =>
                {
                    var container = new Container();
                    container.RegisterInstance(provider.GetRequiredService<IHeaderCustomerGUID>());
                    return container;
                });
            })
            .Configure(app =>
            {
                app.Use(async (context, next) =>
                {
                    context.Connection.RemoteIpAddress = IPAddress.Parse(RemoteIp);
                    context.Connection.LocalPort = PrivatePort;
                    await next();
                });
                if (!legacyMvc) app.UseRouting();
                if (service == ApiService.Public)
                    app.UseMiddleware<PublicApiTenant>();
                else
                    app.UseMiddleware<StoreCustomerGUIDMiddleware>();
                if (legacyMvc)
                    app.UseMvc();
                else
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
            })).Start();
        return new ApiServer(host);
    }

    public static async Task<ApiReply> SendAsync(ApiServer server, string method, string path, object body = null,
        string tenant = null, Dictionary<string, string> headers = null, bool useHttps = true)
    {
        using var client = server.CreateClient();
        client.BaseAddress = new Uri(useHttps ? "https://localhost" : "http://localhost");
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        foreach (var header in RequestHeaders(tenant, headers))
            request.Headers.Add(header.Key, header.Value);
        if (body != null)
            request.Content = new StringContent(BodyText(body), Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request);
        return new ApiReply((int)response.StatusCode, await response.Content.ReadAsStringAsync(),
            response.Headers.CacheControl?.ToString(), response.Content.Headers.ContentType?.MediaType);
    }

    public async Task<ApiReply> SendLambdaAsync(ApiService service, string method, string path, object body = null,
        string tenant = null, Dictionary<string, string> headers = null, bool base64 = false)
    {
        using var provider = services.BuildServiceProvider();
        var requestHeaders = RequestHeaders(tenant, headers);
        var bodyText = body == null ? null : BodyText(body);
        if (base64)
            bodyText = Convert.ToBase64String(Encoding.UTF8.GetBytes(bodyText));

        var uri = new Uri(new Uri("https://localhost"), path);
        if (service == ApiService.Public)
        {
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in query)
                parameters.Add(pair.Key, pair.Value.ToString());

            // These constructors configure environment-backed production dependencies.
            // The handlers use the static service provider, supplied here with test dependencies.
            var handler = (PublicLambda)RuntimeHelpers.GetUninitializedObject(typeof(PublicLambda));
            var previous = PublicLambda.services;
            try
            {
                PublicLambda.services = provider;
                var reply = await handler.FunctionHandler(new APIGatewayProxyRequest
                {
                    Path = uri.AbsolutePath, HttpMethod = method, Headers = requestHeaders,
                    QueryStringParameters = parameters, Body = bodyText, IsBase64Encoded = base64
                }, LambdaContext);
                return new ApiReply(reply.StatusCode, reply.Body, Header(reply.Headers, "Cache-Control"));
            }
            finally
            {
                PublicLambda.services = previous;
            }
        }

        var request = new APIGatewayHttpApiV2ProxyRequest
        {
            RawPath = uri.AbsolutePath, Headers = requestHeaders, Body = bodyText, IsBase64Encoded = base64,
            RequestContext = new APIGatewayHttpApiV2ProxyRequest.ProxyRequestContext
            {
                Http = new APIGatewayHttpApiV2ProxyRequest.HttpDescription { Method = method, SourceIp = RemoteIp }
            }
        };
        if (service == ApiService.Persistence)
        {
            var handler = (PersistenceLambda)RuntimeHelpers.GetUninitializedObject(typeof(PersistenceLambda));
            var previous = PersistenceLambda.services;
            try
            {
                PersistenceLambda.services = provider;
                var reply = await handler.FunctionHandler(request, LambdaContext);
                return new ApiReply(reply.StatusCode, reply.Body, Header(reply.Headers, "Cache-Control"));
            }
            finally
            {
                PersistenceLambda.services = previous;
            }
        }

        Assert.Equal(ApiService.Instance, service);
        var instanceHandler = (InstanceLambda)RuntimeHelpers.GetUninitializedObject(typeof(InstanceLambda));
        var previousInstanceServices = InstanceLambda.services;
        try
        {
            InstanceLambda.services = provider;
            var reply = await instanceHandler.FunctionHandler(request, LambdaContext);
            return new ApiReply(reply.StatusCode, reply.Body, Header(reply.Headers, "Cache-Control"));
        }
        finally
        {
            InstanceLambda.services = previousInstanceServices;
        }
    }

    public async Task VerifyAsync(ApiService service, string method, string path, object body, Action<ApiReply> verify,
        Dictionary<string, string> headers = null, bool alsoLambda = true)
    {
        using var server = Start(service);
        verify(await SendAsync(server, method, path, body, headers: headers));
        if (alsoLambda)
            verify(await SendLambdaAsync(service, method, path, body, headers: headers));
    }

    private static string BodyText(object body) => body is string raw ? raw : JsonSerializer.Serialize(body);

    private static Dictionary<string, string> RequestHeaders(string tenant, Dictionary<string, string> headers)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (tenant != "")
            values.Add("X-CustomerGUID", tenant ?? Tenant.ToString());
        if (headers != null)
            foreach (var header in headers)
                values[header.Key] = header.Value;
        return values;
    }

    private static string Header(IDictionary<string, string> headers, string name) =>
        headers.TryGetValue(name, out var value) ? value : null;

    private sealed class LambdaLogger : ILambdaLogger
    {
        public void Log(string message) { }
        public void LogLine(string message) { }
    }
}

internal sealed class ApiServer(IHost host) : IDisposable
{
    public HttpClient CreateClient() => host.GetTestClient();
    public IServiceProvider Services => host.Services;
    public void Dispose() => host.Dispose();
}

internal sealed record ApiReply(int Status, string Body, string CacheControl, string ContentType = null)
{
    public JsonElement Json(int status = 200)
    {
        Is(status);
        using var document = JsonDocument.Parse(Body);
        return document.RootElement.Clone();
    }

    public void Is(int status) => Assert.True(Status == status, $"Expected HTTP {status}, got {Status}. Body: {Body}");

    public void Success(int status = 200) => Assert.True(Json(status).Field("Success").GetBoolean());
}

internal static class JsonAssertions
{
    public static JsonElement Field(this JsonElement json, string name)
    {
        foreach (var property in json.EnumerateObject())
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                return property.Value;

        throw new InvalidOperationException($"Expected JSON property {name} in {json}.");
    }
}

internal sealed class EnvironmentSetting : IDisposable
{
    private readonly string name;
    private readonly string previous;

    public EnvironmentSetting(string name, string value)
    {
        this.name = name;
        previous = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
    }

    public void Dispose() => Environment.SetEnvironmentVariable(name, previous);
}
