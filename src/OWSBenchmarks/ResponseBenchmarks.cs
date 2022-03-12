using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using OWSPublicAPI;
using System.Net.Http;
using System.Threading.Tasks;

namespace OWSBenchmarks
{
    [InProcess]
    [MemoryDiagnoser]
    public class ResponseBenchmarks
    {
        private HttpClient client;
        private StringContent postContent;
        //GetUserSession userSession;
        //StringContent userSessionStringContent;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var factory = new WebApplicationFactory<Startup>()
                .WithWebHostBuilder(configuration =>
                {
                    configuration.ConfigureLogging((hostingContext, logging) =>
                    {
                        logging.ClearProviders();
                    });
                });

            client = factory.CreateClient();

            //client.DefaultRequestHeaders.Add("Content-Type", "application/json");
            client.DefaultRequestHeaders.Add("X-CustomerGUID", "EEE65F97-BAB1-482E-8439-9A14AE7366B5");
            //client.DefaultRequestHeaders.Add("User-Agent", "Fiddler");

            postContent = new StringContent("");
        }

        [Benchmark]
        public Task GetUserSessionTime()
        {
            return client.GetAsync("https://localhost:44303/api/Users/GetUserSession?UserSessionGUID=147DBA25-5689-42A4-A52D-8621F17BB99D");

            //return client.PostAsync("http://localhost:52611/RPGUser/GetUserSession/147DBA25-5689-42A4-A52D-8621F17BB99D?CustomerGUID=EEE65F97-BAB1-482E-8439-9A14AE7366B5", postContent);
        }
    }
}
