using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OWSCharacterPersistence.Controllers;
using OWSCharacterPersistence.Requests.Characters;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using OWSShared.Implementations;
using Xunit;
using Function = OWSCharacterPersistenceLambda.Function;

namespace OWSTests.Characters
{
    public class PersistenceGetByIdTests
    {
        private readonly Guid _customer = Guid.NewGuid();
        private bool _customDataRead;

        private ICharactersRepository Repository(bool missing = false, bool emptyCustomData = false)
        {
            return RepositoryProxy.Create<ICharactersRepository>((method, args) =>
            {
                Assert.Equal(_customer, args[0]);
                if (method.Name == nameof(ICharactersRepository.GetCharByCharName))
                {
                    Assert.Equal("ResolvedName", args[1]);
                    return Task.FromResult(new GetCharByCharName { CharacterId = 42, CharName = "ResolvedName" });
                }
                if (method.Name == nameof(ICharactersRepository.GetCharByCharId))
                {
                    Assert.Equal(42, args[1]);
                    return Task.FromResult(missing ? null : new GetCharByCharName
                    {
                        CharacterId = 42, CharName = "ResolvedName"
                    });
                }

                Assert.Equal(nameof(ICharactersRepository.GetCustomCharacterData), method.Name);
                Assert.Equal("ResolvedName", args[1]);
                _customDataRead = true;
                return Task.FromResult<IEnumerable<CustomCharacterData>>(emptyCustomData
                    ? Array.Empty<CustomCharacterData>()
                    : new[]
                    {
                        new CustomCharacterData { CustomFieldName = "Public", FieldValue = "visible" },
                        new CustomCharacterData { CustomFieldName = "Private", FieldValue = "internal" },
                        new CustomCharacterData { CustomFieldName = "ReadOnlyCharacterData", FieldValue = "stored" }
                    });
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ControllerReturnsAllStoredCustomData(bool emptyCustomData)
        {
            var controller = new CharactersController(Repository(emptyCustomData: emptyCustomData),
                new HeaderCustomerGUID { CustomerGUID = _customer });
            var result = Assert.IsType<OkObjectResult>(await controller.GetById(42));
            var data = Assert.IsType<CharacterAndCustomData>(result.Value);
            Assert.Equal(42, data.CharacterData.CharacterId);
            Assert.True(_customDataRead);
            if (emptyCustomData)
                Assert.Empty(data.CustomCharacterDataRows);
            else
                Assert.Collection(data.CustomCharacterDataRows,
                    row => { Assert.Equal("Public", row.CustomFieldName); Assert.Equal("visible", row.FieldValue); },
                    row => { Assert.Equal("Private", row.CustomFieldName); Assert.Equal("internal", row.FieldValue); },
                    row => { Assert.Equal("ReadOnlyCharacterData", row.CustomFieldName); Assert.Equal("stored", row.FieldValue); });
        }

        [Theory]
        [InlineData(0, false, false, 400)]
        [InlineData(-1, false, false, 400)]
        [InlineData(42, true, false, 400)]
        [InlineData(42, false, true, 401)]
        public async Task HandlerRejectsInvalidLookups(int id, bool missing, bool missingCustomer, int expectedStatus)
        {
            var request = new GetByIdRequest(id);
            request.SetData(Repository(missing), new HeaderCustomerGUID
            {
                CustomerGUID = missingCustomer ? Guid.Empty : _customer
            });
            var result = await request.Handle();
            int? status = result is ObjectResult obj ? obj.StatusCode : ((StatusCodeResult)result).StatusCode;
            Assert.Equal(expectedStatus, status);
            Assert.False(_customDataRead);
        }

        [Theory]
        [InlineData("/api/Characters/42", "POST", false, false, 200)]
        [InlineData("/stage/API/characters/42/", "POST", false, false, 200)]
        [InlineData("/api/Characters/42", "GET", false, false, 405)]
        [InlineData("/api/Characters/42", "POST", true, false, 400)]
        [InlineData("/api/Characters/0", "POST", false, false, 400)]
        [InlineData("/api/Characters/42", "POST", false, true, 401)]
        [InlineData("/api/Other/42", "POST", false, false, 404)]
        [InlineData("/api/Characters/GetByName", "POST", false, false, 200)]
        public async Task LambdaRoutesLookupAndPreservesStatus(string path, string method,
            bool missing, bool missingCustomer, int expectedStatus)
        {
            var previousServices = Function.services;
            using var services = new ServiceCollection()
                .AddSingleton<ICharactersRepository>(Repository(missing))
                .BuildServiceProvider();
            try
            {
                // Supply test services directly, bypassing production connection-string setup.
                Function.services = services;
                var function = (Function)RuntimeHelpers.GetUninitializedObject(typeof(Function));
                var response = await function.FunctionHandler(new APIGatewayHttpApiV2ProxyRequest
                {
                    RawPath = path,
                    RequestContext = new APIGatewayHttpApiV2ProxyRequest.ProxyRequestContext
                    {
                        Http = new APIGatewayHttpApiV2ProxyRequest.HttpDescription { Method = method }
                    },
                    Body = "{\"CharacterName\":\"ResolvedName\"}",
                    Headers = new Dictionary<string, string>
                    {
                        ["x-customerguid"] = missingCustomer ? "invalid" : _customer.ToString()
                    }
                }, null);
                Assert.Equal(expectedStatus, response.StatusCode);
                if (expectedStatus == 200 && path.EndsWith("GetByName"))
                    Assert.False(_customDataRead);
                else if (expectedStatus == 200)
                {
                    using var json = JsonDocument.Parse(response.Body);
                    Assert.Equal(42, json.RootElement.GetProperty("CharacterData").GetProperty("CharacterId").GetInt32());
                    Assert.Equal(3, json.RootElement.GetProperty("CustomCharacterDataRows").GetArrayLength());
                    Assert.Equal("internal", json.RootElement.GetProperty("CustomCharacterDataRows")[1].GetProperty("FieldValue").GetString());
                }
            }
            finally
            {
                Function.services = previousServices;
            }
        }
    }
}
