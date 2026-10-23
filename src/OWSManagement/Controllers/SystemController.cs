using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OWSData.Repositories.Interfaces;
using OWSShared.Options;

namespace OWSManagement.Controllers
{
    /// <summary>Service health, the newest backup, applied migrations and links to the monitoring tools.</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SystemController : Controller
    {
        private static readonly string[] Services =
        {
            "owspublicapi", "owsinstancemanagement", "owscharacterpersistence", "owsglobaldata", "owschat", "owsparty"
        };

        private readonly IConsoleQueriesRepository _queries;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<ManagementOptions> _options;

        public SystemController(IConsoleQueriesRepository queries, IHttpClientFactory httpClientFactory, IOptions<ManagementOptions> options)
        {
            _queries = queries;
            _httpClientFactory = httpClientFactory;
            _options = options;
        }

        [HttpGet]
        [Route("Status")]
        [AllowAnonymous]
        public IActionResult Status()
        {
            return Ok(true);
        }

        [HttpGet]
        [Route("Info")]
        public async Task<IActionResult> Info()
        {
            HttpClient client = _httpClientFactory.CreateClient("Health");
            var health = await Task.WhenAll(Services.Select(async service =>
            {
                DateTime started = DateTime.UtcNow;
                try
                {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    using HttpResponseMessage response = await client.GetAsync($"http://{service}/health", timeout.Token);
                    return new { Service = service, Healthy = response.IsSuccessStatusCode, Status = (int)response.StatusCode, Ms = (int)(DateTime.UtcNow - started).TotalMilliseconds };
                }
                catch (Exception)
                {
                    return new { Service = service, Healthy = false, Status = 0, Ms = (int)(DateTime.UtcNow - started).TotalMilliseconds };
                }
            }));

            FileInfo newestBackup = null;
            string backupDir = _options.Value.BackupDir;
            if (!string.IsNullOrEmpty(backupDir) && Directory.Exists(backupDir))
            {
                newestBackup = new DirectoryInfo(backupDir).GetFiles("*.dump").OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();
            }

            return Ok(new
            {
                Commit = Environment.GetEnvironmentVariable("OWS_GIT_COMMIT") ?? "",
                Services = health,
                Backup = newestBackup == null ? null : new { newestBackup.Name, Bytes = newestBackup.Length, WrittenAt = newestBackup.LastWriteTimeUtc },
                Migrations = await _queries.GetMigrations(10),
                Links = new { _options.Value.GrafanaUrl, _options.Value.SentryUrl }
            });
        }
    }
}
