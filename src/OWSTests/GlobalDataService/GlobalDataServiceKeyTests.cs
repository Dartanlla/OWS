using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using OWSGlobalData.Controllers;
using OWSGlobalData.DTOs;
using OWSShared;
using OWSShared.Interfaces;
using Xunit;

namespace OWSTests.GlobalDataService
{
    internal static class GlobalDataServiceKeyEnvironment
    {
        public const string Key = "test-service-key";

        // SamsaraServiceKeyCheck reads the env var once, so it is set before any test touches it.
        [ModuleInitializer]
        internal static void SetServiceKey()
        {
            Environment.SetEnvironmentVariable("OWS_SAMSARA_SERVICE_KEY", Key);
        }
    }

    /// <summary>
    /// The CustomerGUID ships with game clients, so a global data write must also carry the zone servers'
    /// X-Samsara-Service-Key. Reads stay open to anyone with the CustomerGUID.
    /// </summary>
    public class GlobalDataServiceKeyTests
    {
        private sealed class RecordingGlobalDataRepository : IGlobalDataRepository
        {
            public List<GlobalData> Writes { get; } = new List<GlobalData>();

            public Task AddOrUpdateGlobalData(GlobalData globalData)
            {
                Writes.Add(globalData);
                return Task.CompletedTask;
            }

            public Task<GlobalData> GetGlobalDataByGlobalDataKey(Guid customerGuid, string globalDataKey) =>
                Task.FromResult(new GlobalData { CustomerGuid = customerGuid, GlobalDataKey = globalDataKey, GlobalDataValue = "v" });
        }

        private sealed class HeaderCustomerGUID : IHeaderCustomerGUID
        {
            public Guid CustomerGUID { get; set; } = Guid.NewGuid();
        }

        private static GlobalDataController Controller(RecordingGlobalDataRepository repository, string serviceKey)
        {
            var httpContext = new DefaultHttpContext();
            if (serviceKey != null) httpContext.Request.Headers[SamsaraServiceKeyCheck.Header] = serviceKey;
            return new GlobalDataController(repository, new HeaderCustomerGUID())
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };
        }

        private static AddOrUpdateGlobalDataItemDTO Item() =>
            new AddOrUpdateGlobalDataItemDTO { GlobalDataKey = "k", GlobalDataValue = "v" };

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("wrong-key")]
        public async Task AddOrUpdateGlobalDataItem_Returns401AndDoesNotWrite_WithoutTheServiceKey(string serviceKey)
        {
            var repository = new RecordingGlobalDataRepository();

            var result = await Controller(repository, serviceKey).AddOrUpdateGlobalDataItem(Item());

            Assert.IsType<UnauthorizedResult>(result.Result);
            Assert.Empty(repository.Writes);
        }

        [Fact]
        public async Task AddOrUpdateGlobalDataItem_Writes_WithTheServiceKey()
        {
            var repository = new RecordingGlobalDataRepository();

            var result = await Controller(repository, GlobalDataServiceKeyEnvironment.Key).AddOrUpdateGlobalDataItem(Item());

            Assert.True(result.Value.Success);
            Assert.Single(repository.Writes);
        }

        [Fact]
        public async Task GetGlobalDataItem_DoesNotNeedTheServiceKey()
        {
            var result = await Controller(new RecordingGlobalDataRepository(), null).GetGlobalDataItem("k");

            Assert.Equal("k", result.GlobalDataKey);
        }
    }
}
