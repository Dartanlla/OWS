using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using OWSData.Models.Composites;
using OWSData.Models.Content;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using Xunit;
using static OWSTests.Api.ApiTestHost;

namespace OWSTests.Api;

[Collection(ApiTestCollection.Name)]
public class DataAndContentApiTests
{
    [Fact]
    public async Task GlobalDataCanBeUpdatedAndReadByTenantAndEncodedKey()
    {
        const string key = "Harbor weather";
        var values = new Dictionary<(Guid, string), GlobalData>();
        var host = new ApiTestHost();
        host.Add<IGlobalDataRepository>((method, args) =>
        {
            if (method.Name == nameof(IGlobalDataRepository.AddOrUpdateGlobalData))
            {
                var row = Assert.IsType<GlobalData>(Assert.Single(args));
                Assert.Equal(Tenant, row.CustomerGuid);
                Assert.Equal(key, row.GlobalDataKey);
                values[(row.CustomerGuid, row.GlobalDataKey)] = row;
                return Task.CompletedTask;
            }

            Assert.Equal(nameof(IGlobalDataRepository.GetGlobalDataByGlobalDataKey), method.Name);
            Assert.Equal(key, args[1]);
            return Task.FromResult(values.GetValueOrDefault(((Guid)args[0], (string)args[1])));
        });
        using var server = host.Start(ApiService.GlobalData);
        const string read = "/api/GlobalData/GetGlobalDataItem/Harbor%20weather";
        const string write = "/api/GlobalData/AddOrUpdateGlobalDataItem";
        (await SendAsync(server, "GET", read)).Is(204);
        foreach (string value in new[] { "clear", "storm" })
        {
            (await SendAsync(server, "POST", write, new { GlobalDataKey = key, GlobalDataValue = value })).Success();
            var row = (await SendAsync(server, "GET", read)).Json();
            Assert.Equal(key, row.Field("GlobalDataKey").GetString());
            Assert.Equal(value, row.Field("GlobalDataValue").GetString());
        }
        (await SendAsync(server, "GET", read, tenant: Owner.ToString())).Is(204);
        (await SendAsync(server, "GET", read, tenant: "")).Is(401);
        (await SendAsync(server, "POST", write, new { GlobalDataKey = key, GlobalDataValue = "snow" }, tenant: "")).Is(401);
        Assert.Equal("storm", values[(Tenant, key)].GlobalDataValue);
    }

    [Fact]
    public async Task ContentCacheReturnsRawZoneJsonAndDistinguishesMissingZones()
    {
        var host = new ApiTestHost();
        var content = new Dictionary<string, string>();
        host.Add<IContentCacheRepository>((method, args) =>
        {
            var zone = Assert.IsType<string>(args[0]);
            if (method.Name == nameof(IContentCacheRepository.AddOrUpdateContentCacheAsync))
            {
                Assert.Equal("Harbor District", zone);
                content[zone] = Assert.IsType<string>(args[1]);
                return Task.CompletedTask;
            }

            Assert.Equal(nameof(IContentCacheRepository.GetContentCacheAsync), method.Name);
            return Task.FromResult(content.GetValueOrDefault(zone));
        });

        using var server = host.Start(ApiService.Content);
        const string path = "/api/ContentCache/";
        const string read = path + "GetContentCache?zoneName=Harbor%20District";
        const string write = path + "AddOrUpdateContentCache";
        (await SendAsync(server, "GET", path + "GetContentCache")).Is(400);
        (await SendAsync(server, "GET", read)).Is(404);
        (await SendAsync(server, "POST", write, new { ZoneName = "" })).Is(400);
        (await SendAsync(server, "POST", write, new { ZoneName = "Harbor District" })).Is(400);
        Assert.Empty(content);

        foreach (var version in new[] { 1, 2 })
        {
            var response = await SendAsync(server, "POST", write,
                new { ZoneName = "Harbor District", ContentJson = new { version, props = new[] { "Crate", "Lamp" } } });
            response.Is(200);
            var loaded = await SendAsync(server, "GET", read);
            Assert.Equal("application/json", loaded.ContentType);
            var cache = loaded.Json();
            Assert.Equal(version, cache.GetProperty("version").GetInt32());
            Assert.Equal("Lamp", cache.GetProperty("props")[1].GetString());
        }
        (await SendAsync(server, "GET", path + "GetContentCache?zoneName=Other")).Is(404);
    }

    [Fact]
    public async Task ContentFeatureRoundTripsTheJsonAndMetadata()
    {
        var host = new ApiTestHost();
        ContentFeature stored = null;
        host.Add<IContentRepository>((method, args) =>
        {
            Assert.Equal(Tenant, args[0]);
            if (method.Name == nameof(IContentRepository.AddOrUpdateContentFeatureAsync))
            {
                stored = Assert.IsType<ContentFeature>(args[1]);
                Assert.Equal(Tenant, stored.CustomerGuid);
                Assert.Equal(Owner, stored.CreatedBy);
                Assert.Equal(8, stored.ContentPackageId);
                stored.ContentFeatureId = 25;
                return Task.FromResult(25);
            }

            Assert.Equal(nameof(IContentRepository.GetContentFeatureAsync), method.Name);
            Assert.Equal(25, args[1]);
            return Task.FromResult(stored);
        });
        using var server = host.Start(ApiService.Content);
        var read = $"/api/Content/GetContentFeature?customerGuid={Tenant}&contentFeatureId=25";
        const string write = "/api/Content/AddOrUpdateContentFeature";
        (await SendAsync(server, "GET", read)).Is(404);
        (await SendAsync(server, "POST", write, new { CustomerGuid = Tenant })).Is(400);
        (await SendAsync(server, "POST", write, new { ContentFeatureJson = new { label = "Beacon" } })).Is(400);
        Assert.Null(stored);

        var result = (await SendAsync(server, "POST", write, new
        {
            CustomerGuid = Tenant, ContentFeatureJson = new { label = "Beacon" }, CreatedBy = Owner, ContentPackageId = 8
        })).Json();
        Assert.Equal(25, result.Field("ContentFeatureId").GetInt32());
        var feature = (await SendAsync(server, "GET", read)).Json();
        Assert.Equal(25, feature.Field("ContentFeatureId").GetInt32());
        Assert.Equal(Owner, feature.Field("CreatedBy").GetGuid());
        using var json = JsonDocument.Parse(feature.Field("ContentFeatureJson").GetString());
        Assert.Equal("Beacon", json.RootElement.GetProperty("label").GetString());
    }

    [Fact]
    public async Task ContentPackageRoundTripsVersionDeploymentDateAndJson()
    {
        var host = new ApiTestHost();
        ContentPackage stored = null;
        var deployAt = new DateTime(2026, 12, 1, 10, 30, 0, DateTimeKind.Utc);
        host.Add<IContentRepository>((method, args) =>
        {
            Assert.Equal(Tenant, args[0]);
            if (method.Name == nameof(IContentRepository.AddOrUpdateContentPackageAsync))
            {
                stored = Assert.IsType<ContentPackage>(args[1]);
                Assert.Equal(Tenant, stored.CustomerGuid);
                Assert.Equal(4, stored.VersionNumber);
                Assert.Equal(deployAt, stored.DeployAt);
                stored.ContentPackageId = 8;
                return Task.FromResult(8);
            }

            Assert.Equal(nameof(IContentRepository.GetContentPackageAsync), method.Name);
            Assert.Equal(8, args[1]);
            return Task.FromResult(stored);
        });
        using var server = host.Start(ApiService.Content);
        var read = $"/api/Content/GetContentPackage?customerGuid={Tenant}&contentPackageId=8";
        const string write = "/api/Content/AddOrUpdateContentPackage";
        (await SendAsync(server, "GET", read)).Is(404);
        (await SendAsync(server, "POST", write, new { CustomerGuid = Tenant })).Is(400);
        (await SendAsync(server, "POST", write, new { AggregatedContentJson = new[] { 25 } })).Is(400);
        Assert.Null(stored);

        var result = (await SendAsync(server, "POST", write,
            new { CustomerGuid = Tenant, VersionNumber = 4, DeployAt = deployAt, AggregatedContentJson = new[] { 25 } })).Json();
        Assert.Equal(8, result.Field("ContentPackageId").GetInt32());
        var package = (await SendAsync(server, "GET", read)).Json();
        Assert.Equal(4, package.Field("VersionNumber").GetInt32());
        Assert.Equal(deployAt, package.Field("DeployAt").GetDateTime());
        using var json = JsonDocument.Parse(package.Field("AggregatedContentJson").GetString());
        Assert.Equal(25, Assert.Single(json.RootElement.EnumerateArray()).GetInt32());
    }

    [Theory]
    [InlineData("GET", "Get", null, "Example Get Request")]
    [InlineData("POST", "Post", """{"Message":"Hello"}""", "Example Post Request Hello")]
    public async Task ContentExamplesReturnTheirDocumentedText(string method, string action, string body, string expected)
    {
        using var server = new ApiTestHost().Start(ApiService.Content);
        var reply = await SendAsync(server, method, "/api/Example/" + action, body);
        reply.Is(200);
        Assert.Equal(expected, reply.Body);
    }

    [Fact]
    public async Task ManagementListsCreatesAndEditsUsersForTheSelectedTenant()
    {
        const string path = "/api/Users";
        var host = new ApiTestHost();
        var users = new List<User>();
        host.Add<IUsersRepository>((method, args) =>
        {
            switch (method.Name)
            {
                case nameof(IUsersRepository.GetUsers):
                    Assert.Equal(Tenant, Assert.Single(args));
                    return Task.FromResult<IEnumerable<User>>(users);
                case nameof(IUsersRepository.RegisterUser):
                    Assert.Equal(new object[] { Tenant, "aster@example.test", "test-password", "Aster", "Example" }, args);
                    users.Add(new User { CustomerGuid = Tenant, UserGuid = Owner, Email = "aster@example.test", FirstName = "Aster", LastName = "Example" });
                    break;
                case nameof(IUsersRepository.UpdateUser):
                    Assert.Equal(new object[] { Tenant, Owner, "Aster", "Updated", "updated@example.test" }, args);
                    var user = Assert.Single(users);
                    user.LastName = "Updated";
                    user.Email = "updated@example.test";
                    break;
                default:
                    throw new InvalidOperationException(method.Name);
            }
            return Task.FromResult(new SuccessAndErrorMessage { Success = true });
        });
        using var server = host.Start(ApiService.Management);
        var add = new { Email = "aster@example.test", Password = "test-password", FirstName = "Aster", LastName = "Example" };
        var edit = new { UserGUID = Owner, FirstName = "Aster", LastName = "Updated", Email = "updated@example.test" };
        (await SendAsync(server, "GET", path, tenant: "")).Is(401);
        (await SendAsync(server, "POST", path, add, tenant: "")).Is(401);
        (await SendAsync(server, "PUT", path, edit, tenant: "")).Is(401);
        Assert.Empty((await SendAsync(server, "GET", path)).Json().EnumerateArray());

        (await SendAsync(server, "POST", path, add)).Success();
        var created = Assert.Single((await SendAsync(server, "GET", path)).Json().EnumerateArray());
        Assert.Equal(Owner, created.Field("UserGuid").GetGuid());
        Assert.Equal("aster@example.test", created.Field("Email").GetString());
        (await SendAsync(server, "PUT", path, edit)).Success();
        var edited = Assert.Single((await SendAsync(server, "GET", path)).Json().EnumerateArray());
        Assert.Equal(Owner, edited.Field("UserGuid").GetGuid());
        Assert.Equal("updated@example.test", edited.Field("Email").GetString());
        Assert.Equal("Updated", edited.Field("LastName").GetString());
    }
}
