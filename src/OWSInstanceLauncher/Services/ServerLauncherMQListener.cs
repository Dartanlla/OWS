using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OWSData.Models;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSShared.Messages;
using OWSShared.Options;
using OWSShared.RequestPayloads;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace OWSInstanceLauncher.Services
{
    public class ServerLauncherMQListener : IInstanceLauncherJob //BackgroundService
    {
        private IMessagingChannel serverSpinUpChannel;
        private IMessagingChannel serverShutDownChannel;
        private string serverSpinUpQueueName;
        private string serverShutDownQueueName;

        private readonly Guid _customerGUID;
        private readonly Guid _launcherGUID;
        private readonly IWritableOptions<OWSInstanceLauncherOptions> _owsInstanceLauncherOptions;
        private readonly IOptions<APIPathOptions> _owsAPIPathOptions;
        private readonly IInstanceLauncherMessaging _instanceLauncherMessaging;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IZoneServerProcessesRepository _zoneServerProcessesRepository;
        private readonly IOWSInstanceLauncherDataRepository _owsInstanceLauncherDataRepository;
        private readonly int _worldServerId;
        private readonly string _serverIP;
        private readonly int _MaxNumberOfInstances;
        private readonly string _InternalServerIP;
        private readonly int _StartingInstancePort;

        public ServerLauncherMQListener(IWritableOptions<OWSInstanceLauncherOptions> owsInstanceLauncherOptions, IOptions<APIPathOptions> owsAPIPathOptions, IInstanceLauncherMessaging instanceLauncherMessaging, IHttpClientFactory httpClientFactory, IZoneServerProcessesRepository zoneServerProcessesRepository,
            IOWSInstanceLauncherDataRepository owsInstanceLauncherDataRepository)
        {
            _owsInstanceLauncherOptions = owsInstanceLauncherOptions;
            _owsAPIPathOptions = owsAPIPathOptions;
            _instanceLauncherMessaging = instanceLauncherMessaging;
            _httpClientFactory = httpClientFactory;
            _zoneServerProcessesRepository = zoneServerProcessesRepository;
            _owsInstanceLauncherDataRepository = owsInstanceLauncherDataRepository;
            _customerGUID = Guid.Parse(owsInstanceLauncherOptions.Value.OWSAPIKey);
            if (String.IsNullOrEmpty(owsInstanceLauncherOptions.Value.LauncherGuid))
            {
                _launcherGUID = Guid.NewGuid();
                _owsInstanceLauncherOptions.Update(x => x.LauncherGuid = _launcherGUID.ToString());
                Log.Information($"New Launcher GUID Generated: {_launcherGUID}");
            }
            else
            {
                _launcherGUID = Guid.Parse(owsInstanceLauncherOptions.Value.LauncherGuid);
            }
            _serverIP = owsInstanceLauncherOptions.Value.ServerIP;
            _MaxNumberOfInstances = owsInstanceLauncherOptions.Value.MaxNumberOfInstances;
            _InternalServerIP = owsInstanceLauncherOptions.Value.InternalServerIP;
            _StartingInstancePort = owsInstanceLauncherOptions.Value.StartingInstancePort;

            RegisterLauncher();

            _worldServerId = GetWorldServerID();

            InitRabbitMQ();
        }

        public void RegisterLauncher()
        {
            Log.Information($"Attempting to register Launcher GUID: {_launcherGUID}");
            var isregistered = RegisterInstanceLauncherRequest();

            if (isregistered == 1)
            {
                Log.Information($"Success!  Registered: {_launcherGUID}");
            }
            else
            {
                Log.Error($"Error Registering: {_launcherGUID}");
            }
        }

        /*
        private Guid GetLauncherGuid()
        {
            return Guid.Parse(File.ReadAllText("Guid.txt"));
        }
        */

        private int GetWorldServerID()
        {
            int worldServerID = _owsInstanceLauncherDataRepository.GetWorldServerID();

            if (worldServerID < 1)
            {
                worldServerID = StartInstanceLauncherRequest();
                _owsInstanceLauncherDataRepository.SetWorldServerID(worldServerID);
            }

            return worldServerID;
        }

        private void InitRabbitMQ()
        {
            Log.Information("Attempting to Register OWS Instance Launcher with RabbitMQ ServerSpinUp Queue...");

            try
            {
                _instanceLauncherMessaging.InitializeConnection();
            }
            catch (Exception ex)
            {
                Log.Error($"Error connecting to RabbitMQ.  Check that your can access RabbitMQ from your browser by going to: http://host.docker.internal:15672/  Use default username / password: dev / test.  If the page doesn't load check your Windows HOSTs file for the three entries that Docker Desktop is supposed to add on installation.  If the page does load, but you can't login, you probably have another copy of RabbitMQ already running.  Disable your copy of RabbitMQ and try again.  Error message: {ex.Message}");
                return;
            }

            (serverSpinUpChannel, serverSpinUpQueueName) = _instanceLauncherMessaging.InitializeSpinUpChannel(_worldServerId);
            (serverShutDownChannel, serverShutDownQueueName) = _instanceLauncherMessaging.InitializeShutDownChannel(_worldServerId);

            _instanceLauncherMessaging.GetConnection().ConnectionShutdown += OnConnectionShutdown;

            Log.Information("Registered OWS Instance Launcher with RabbitMQ ServerSpinUp Queue Success!");
        }

        //protected override Task ExecuteAsync(CancellationToken stoppingToken)
        public void DoWork()
        {
            //This will be null if there was a problem with initialization in the constructor
            if (_owsInstanceLauncherOptions == null)
            {
                return;
            }

            serverSpinUpChannel.StartConsuming(serverSpinUpQueueName, body =>
            {
                MQSpinUpServerMessage msg = MQSpinUpServerMessage.Deserialize(body);
                Log.Information($"Server Spin Up Message Received: {msg.CustomerGUID} WorldServerID: {msg.WorldServerID} ZoneInstanceID: {msg.ZoneInstanceID} MapName: {msg.MapName} Port: {msg.Port}");
                HandleServerSpinUpMessage(msg.CustomerGUID, msg.WorldServerID, msg.ZoneInstanceID, msg.MapName, msg.Port);
                return Task.CompletedTask;
            });

            serverShutDownChannel.StartConsuming(serverShutDownQueueName, body =>
            {
                Log.Information("Message Received");
                MQShutDownServerMessage msg = MQShutDownServerMessage.Deserialize(body);
                HandleServerShutDownMessage(msg.CustomerGUID, msg.ZoneInstanceID);
                return Task.CompletedTask;
            });
        }

        private void HandleServerSpinUpMessage(Guid customerGUID, int worldServerID, int zoneInstanceID, string mapName, int port)
        {
            Log.Information($"Starting up {customerGUID} : {worldServerID} : {mapName} : {port}");

            //Security Check on CustomerGUID
            if (_customerGUID != customerGUID)
            {
                Log.Error("HandleServerSpinUpMessage - Incoming CustomerGUID does not match OWSAPIKey in appsettings.json");
                return;
            }

            //string PathToDedicatedServer = "E:\\Program Files\\Epic Games\\UE_4.25\\Engine\\Binaries\\Win64\\UE4Editor.exe";
            //string ServerArguments = "\"C:\\OWS\\OpenWorldStarterPlugin\\OpenWorldStarter.uproject\" {0}?listen -server -log -nosteam -messaging -port={1}";

            string serverArguments = (_owsInstanceLauncherOptions.Value.IsServerEditor ? "\"" + _owsInstanceLauncherOptions.Value.PathToUProject + "\" " : "")
                + "{0}?listen -server "
                + (_owsInstanceLauncherOptions.Value.UseServerLog ? "-log " : "")
                + (_owsInstanceLauncherOptions.Value.UseNoSteam ? "-nosteam " : "")
                + "-port={1} "
                + "-zoneinstanceid={2}";

            System.Diagnostics.Process proc = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _owsInstanceLauncherOptions.Value.PathToDedicatedServer,
                    Arguments = Encoding.Default.GetString(Encoding.UTF8.GetBytes(String.Format(serverArguments, mapName, port, zoneInstanceID))),
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    CreateNoWindow = false
                }
            };

            proc.Start();
            //proc.WaitForInputIdle();

            _zoneServerProcessesRepository.AddZoneServerProcess(new ZoneServerProcess
            {
                ZoneInstanceId = zoneInstanceID,
                MapName = mapName,
                Port = port,
                ProcessId = proc.Id,
                ProcessName = proc.ProcessName
            });

            Log.Information($"{customerGUID} : {worldServerID} : {mapName} : {port} has started.  ProcessId: {proc.Id}, ProcessName: {proc.ProcessName}");

            //The server has finished spinning up.  Set the status to 2.
            //_ = UpdateZoneServerStatusReady(zoneInstanceID);
        }

        private void HandleServerShutDownMessage(Guid customerGUID, int zoneInstanceID)
        {
            Log.Information($"Shutting down {customerGUID} : {zoneInstanceID}");

            //Security Check on CustomerGUID
            if (_customerGUID != customerGUID)
            {
                Log.Error("HandleServerShutDownMessage - Incoming CustomerGUID does not match OWSAPIKey in appsettings.json");
                return;
            }

            int foundProcessId = _zoneServerProcessesRepository.FindZoneServerProcessId(zoneInstanceID);

            if (foundProcessId > 0)
            {
                System.Diagnostics.Process procToKill = System.Diagnostics.Process.GetProcessById(foundProcessId);

                if (procToKill != null)
                {
                    procToKill.Kill();
                }
            }
        }

        private void ShutDownAllZoneServerInstances()
        {
            Log.Information("Shutting down all Server Instances...");

            foreach (var zoneServerInstance in _zoneServerProcessesRepository.GetZoneServerProcesses())
            {
                if (zoneServerInstance.ProcessId > 0)
                {
                    System.Diagnostics.Process procToKill = System.Diagnostics.Process.GetProcessById(zoneServerInstance.ProcessId);

                    if (procToKill != null)
                    {
                        procToKill.Kill();
                    }
                }
            }
        }

        private int RegisterInstanceLauncherRequest()
        {
            try
            {
                var instanceManagementHttpClient = _httpClientFactory.CreateClient("OWSInstanceManagement");

                var RegisterLauncherPayload = new
                {
                    request = new RegisterInstanceLauncherRequestPayload
                    {
                        launcherGUID = _launcherGUID.ToString(),
                        ServerIP = _serverIP,
                        MaxNumberOfInstances = _MaxNumberOfInstances,
                        InternalServerIP = _InternalServerIP,
                        StartingInstancePort = _StartingInstancePort
                    }
                };

                var RegisterLauncherPayloadRequest = new StringContent(JsonSerializer.Serialize(RegisterLauncherPayload), Encoding.UTF8, "application/json");

                var responseMessageAsync = instanceManagementHttpClient.PostAsync("api/Instance/RegisterLauncher", RegisterLauncherPayloadRequest);
                var responseMessage = responseMessageAsync.Result;
                var responseContentAsync = responseMessage.Content.ReadAsStringAsync();

                if (responseMessage == null)
                {
                    return -1;
                }

                if (!responseMessage.IsSuccessStatusCode)
                {
                    return -1;
                }

                return 1;
            }

            catch (Exception ex)
            {
                Log.Error($"Error connecting to Instance Management API: {ex.Message} - {ex.InnerException}");
            }

            return -1;
        }

        private int StartInstanceLauncherRequest()
        {
            try
            {
                var instanceManagementHttpClient = _httpClientFactory.CreateClient("OWSInstanceManagement");

                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(instanceManagementHttpClient.BaseAddress + "api/Instance/StartInstanceLauncher"),
                    Method = HttpMethod.Get
                };
                request.Headers.Add("X-LauncherGUID", _launcherGUID.ToString());
                var responseMessageAsync = instanceManagementHttpClient.SendAsync(request);
                var responseMessage = responseMessageAsync.Result;

                if (responseMessage == null)
                {
                    return -1;
                }

                if (!responseMessage.IsSuccessStatusCode)
                {
                    return -1;
                }

                var responseContentAsync = responseMessage.Content.ReadAsStringAsync();
                string responseContentString = responseContentAsync.Result;

                int worldServerID = -1;
                if (Int32.TryParse(responseContentString, out worldServerID))
                {
                    return worldServerID;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error connecting to Instance Management API: {ex.Message} - {ex.InnerException}");
            }

            return -1;
        }

        private async Task ShutDownInstanceLauncherRequest(int worldServerId)
        {
            var instanceManagementHttpClient = _httpClientFactory.CreateClient("OWSInstanceManagement");

            var worldServerIDRequestPayload = new
            {
                request = new WorldServerIDRequestPayload
                {
                    WorldServerID = worldServerId
                }
            };

            var shutDownInstanceLauncherRequest = new StringContent(JsonSerializer.Serialize(worldServerIDRequestPayload), Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(instanceManagementHttpClient.BaseAddress + "api/Instance/ShutDownInstanceLauncher"),
                Method = HttpMethod.Post,
                Content = shutDownInstanceLauncherRequest
            };
            request.Headers.Add("X-LauncherGUID", _launcherGUID.ToString());

            var responseMessage = await instanceManagementHttpClient.SendAsync(request);

            return;
        }

        private async Task UpdateZoneServerStatusReady(int zoneInstanceID)
        {
            var instanceManagementHttpClient = _httpClientFactory.CreateClient("OWSInstanceManagement");

            var setZoneInstanceStatusRequestPayload = new
            {
                request = new SetZoneInstanceStatusRequestPayload
                {
                    ZoneInstanceID = zoneInstanceID,
                    InstanceStatus = 2 //Ready
                }
            };

            var setZoneInstanceStatusRequest = new StringContent(JsonSerializer.Serialize(setZoneInstanceStatusRequestPayload), Encoding.UTF8, "application/json");

            var responseMessage = await instanceManagementHttpClient.PostAsync("api/Instance/SetZoneInstanceStatus", setZoneInstanceStatusRequest);

            return;
        }

        private void OnConnectionShutdown(object? sender, EventArgs e) { }

        public void Dispose()
        {
            Log.Information("Shutting Down OWS Instance Launcher...");

            if (_worldServerId > 0)
            {
                var shutDownTask = ShutDownInstanceLauncherRequest(_worldServerId);

                shutDownTask.Wait();

                ShutDownAllZoneServerInstances();
            }

            if (serverSpinUpChannel != null)
            {
                serverSpinUpChannel.Close();
            }
            if (serverShutDownChannel != null)
            {
                serverShutDownChannel.Close();
            }
            _instanceLauncherMessaging.GetConnection()?.Close();

            Log.Information("Done!");
        }
    }
}
