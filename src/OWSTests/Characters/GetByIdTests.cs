using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using OWSPublicAPI.DTOs;
using OWSPublicAPI.Requests.Characters;
using OWSPublicAPILambda;
using OWSShared.Implementations;
using OWSShared.Interfaces;
using Xunit;

namespace OWSTests.Characters
{
    public class GetByIdTests
    {
        private readonly Guid _customer = Guid.NewGuid();
        private readonly Guid _session = Guid.NewGuid();
        private readonly Guid _owner = Guid.NewGuid();
        private bool _customDataRead;

        private GetByIdRequest CreateRequest(int id = 42, string session = null,
            bool invalidSession = false, bool missingCharacter = false, bool wrongOwner = false,
            bool missingCustomer = false)
        {
            var users = RepositoryProxy.Create<IUsersRepository>((method, args) =>
            {
                Assert.Equal(nameof(IUsersRepository.GetUserSession), method.Name);
                Assert.Equal(_customer, args[0]);
                Assert.Equal(_session, args[1]);
                return Task.FromResult(invalidSession ? null : new GetUserSession { UserGuid = _owner, CustomerGuid = _customer, UserSessionGUID = _session });
            });
            var characters = RepositoryProxy.Create<ICharactersRepository>((method, args) =>
            {
                Assert.Equal(_customer, args[0]);
                if (method.Name == nameof(ICharactersRepository.GetCharByCharId))
                {
                    Assert.Equal(42, args[1]);
                    return Task.FromResult(missingCharacter ? null : new GetCharByCharName
                    {
                        CharacterId = 42, CustomerGuid = _customer, CharName = "ResolvedName",
                        UserGuid = wrongOwner ? Guid.NewGuid() : _owner
                    });
                }
                Assert.Equal(nameof(ICharactersRepository.GetOwnedCharacterCustomData), method.Name);
                Assert.Equal(42, args[1]);
                Assert.Equal(_owner, args[2]);
                _customDataRead = true;
                return Task.FromResult<IEnumerable<CustomCharacterData>>(new[]
                {
                    new CustomCharacterData { CustomFieldName = "Public", FieldValue = "visible" },
                    new CustomCharacterData { CustomFieldName = "Private", FieldValue = "hidden" }
                });
            });
            return new GetByIdRequest(id, new GetByIdDTO { UserSessionGUID = session ?? _session.ToString() },
                users, characters, new HeaderCustomerGUID { CustomerGUID = missingCustomer ? Guid.Empty : _customer },
                new PublicFields(), new ReadOnlyData());
        }

        [Fact]
        public async Task ReturnsCharacterWithFilteredCustomAndReadOnlyData()
        {
            var result = Assert.IsType<OkObjectResult>(await CreateRequest().Handle());
            var data = Assert.IsType<CharacterAndCustomData>(result.Value);
            Assert.Equal(42, data.CharacterData.CharacterId);
            Assert.Collection(data.CustomCharacterDataRows,
                row => { Assert.Equal("Public", row.CustomFieldName); Assert.Equal("visible", row.FieldValue); },
                row => { Assert.Equal("ReadOnlyCharacterData", row.CustomFieldName); Assert.Equal("readonly", row.FieldValue); });
        }

        [Theory]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        public async Task RejectsInvalidSessionMissingCharacterAndWrongOwner(bool invalidSession, bool missingCharacter, bool wrongOwner)
        {
            var result = await CreateRequest(invalidSession: invalidSession,
                missingCharacter: missingCharacter, wrongOwner: wrongOwner).Handle();
            if (invalidSession)
                Assert.IsType<UnauthorizedResult>(result);
            else
                Assert.IsType<BadRequestObjectResult>(result);
            Assert.False(_customDataRead);
        }

        [Theory]
        [InlineData(0, "bad-guid")]
        [InlineData(-1, "bad-guid")]
        [InlineData(42, "bad-guid")]
        public async Task RejectsInvalidInputBeforeReadingRepositories(int id, string session)
        {
            Assert.IsType<BadRequestObjectResult>(await CreateRequest(id, session).Handle());
        }

        [Fact]
        public async Task RejectsMissingCustomer()
        {
            Assert.IsType<UnauthorizedResult>(await CreateRequest(missingCustomer: true).Handle());
        }

        [Theory]
        [InlineData("/api/Characters/42", "POST", "valid", false, 200)]
        [InlineData("/stage/api/Characters/42/", "POST", "valid", true, 200)]
        [InlineData("/api/Characters/42", "GET", "valid", false, 405)]
        [InlineData("/api/Characters/42", "POST", "{", false, 400)]
        [InlineData("/api/Characters/42", "POST", "null", false, 400)]
        [InlineData("/api/Characters/42", "POST", "{}", false, 400)]
        [InlineData("/api/Other/42", "POST", "valid", false, 404)]
        [InlineData("/api/Characters/42", "POST", "valid", false, 401, "missing")]
        [InlineData("/api/Characters/42", "POST", "valid", false, 401, "invalid")]
        [InlineData("/api/Characters/42", "POST", "valid", false, 401, "unknown")]
        public async Task LambdaRoutesIdAndPreservesStatus(string path, string method, string body, bool base64, int expectedStatus, string tenant = "known")
        {
            // The constructor only configures services; all database access is replaced below.
            string previousConnection = Environment.GetEnvironmentVariable("OWS_DB_CONNECTION_STRING");
            var previousServices = Function.services;
            try
            {
                Environment.SetEnvironmentVariable("OWS_DB_CONNECTION_STRING", "Host=localhost;Database=unused");
                var function = new Function();
                var serviceCollection = new ServiceCollection();
                foreach (var type in new[] { typeof(IUsersRepository), typeof(ICharactersRepository),
                    typeof(IPublicAPIInputValidation), typeof(OWSExternalLoginProviders.Interfaces.IExternalLoginProviderFactory),
                    typeof(Microsoft.Extensions.Options.IOptions<OWSShared.Options.PublicAPIOptions>), typeof(System.Net.Http.IHttpClientFactory),
                    typeof(ICustomDataSelector) })
                {
                    serviceCollection.AddSingleton(type, Function.services.GetRequiredService(type));
                }
                serviceCollection.AddSingleton<IUsersRepository>(RepositoryProxy.Create<IUsersRepository>((m, a) =>
                    m.Name == nameof(IUsersRepository.CustomerExists) ? Task.FromResult((Guid)a[0] == _customer) :
                    Task.FromResult(new GetUserSession { UserGuid = _owner, CustomerGuid = _customer, UserSessionGUID = _session })));
                serviceCollection.AddSingleton<ICharactersRepository>(RepositoryProxy.Create<ICharactersRepository>((m, a) =>
                {
                    if (m.Name == nameof(ICharactersRepository.GetCharByCharId))
                    {
                        Assert.Equal(_customer, a[0]);
                        Assert.Equal(42, a[1]);
                        return Task.FromResult(new GetCharByCharName { CharacterId = 42, CustomerGuid = _customer, CharName = "ResolvedName", UserGuid = _owner });
                    }
                    return Task.FromResult<IEnumerable<CustomCharacterData>>(Array.Empty<CustomCharacterData>());
                }));
                serviceCollection.AddSingleton<ICustomCharacterDataSelector>(new PublicFields());
                serviceCollection.AddSingleton<IGetReadOnlyPublicCharacterData>(new ReadOnlyData());
                using var services = serviceCollection.BuildServiceProvider();
                Function.services = services;
                if (body == "valid") body = JsonSerializer.Serialize(new { UserSessionGUID = _session });
                var response = await function.FunctionHandler(new APIGatewayProxyRequest
                {
                    Path = path, HttpMethod = method,
                    Body = base64 ? Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(body)) : body,
                    IsBase64Encoded = base64,
                    Headers = tenant == "missing" ? new Dictionary<string, string>() :
                        new Dictionary<string, string>
                        {
                            ["x-CuStOmErGuId"] = tenant == "known" ? _customer.ToString() :
                                tenant == "unknown" ? Guid.NewGuid().ToString() : "invalid"
                        }
                }, null);
                Assert.Equal(expectedStatus, response.StatusCode);
                if (expectedStatus == 200)
                {
                    using var json = JsonDocument.Parse(response.Body);
                    Assert.Equal(42, json.RootElement.GetProperty("CharacterData").GetProperty("CharacterId").GetInt32());
                }
            }
            finally
            {
                Function.services = previousServices;
                Environment.SetEnvironmentVariable("OWS_DB_CONNECTION_STRING", previousConnection);
            }
        }

        private class PublicFields : ICustomCharacterDataSelector
        {
            public bool ShouldExportThisCustomCharacterDataField(string fieldName) => fieldName == "Public";
        }

        private class ReadOnlyData : IGetReadOnlyPublicCharacterData
        {
            public Task<string> GetReadOnlyPublicCharacterData(int characterId)
            {
                Assert.Equal(42, characterId);
                return Task.FromResult("readonly");
            }
        }
    }

    public class RepositoryProxy : DispatchProxy
    {
        private Func<MethodInfo, object[], object> _invoke;
        public static T Create<T>(Func<MethodInfo, object[], object> invoke) where T : class
        {
            var proxy = Create<T, RepositoryProxy>();
            ((RepositoryProxy)(object)proxy)._invoke = invoke;
            return proxy;
        }
        protected override object Invoke(MethodInfo targetMethod, object[] args) => _invoke(targetMethod, args);
    }
}
