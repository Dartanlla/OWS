using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OWSData.Models;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSShared.Messages;
using OWSShared.Options;
using OWSShared.RequestPayloads;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OWSShared.Objects
{
    public class ServerLauncherMQListener : IInstanceLauncherJob //BackgroundService
    {
        private IConnection connection;
        private IModel serverSpinUpChannel;
        private IModel serverShutDownChannel;
        private Guid serverSpinUpQueueNameGUID;
        private Guid serverShutDownQueueNameGUID;

        private readonly Guid _customerGUID;
        private readonly IOptions<OWSInstanceLauncherOptions> _owsInstanceLauncherOptions;
        private readonly IOptions<APIPathOptions> _owsAPIPathOptions;
        private readonly IOptions<RabbitMQOptions> _rabbitMQOptions;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IZoneServerProcessesRepository _zoneServerProcessesRepository;
        private readonly IOWSInstanceLauncherDataRepository _owsInstanceLauncherDataRepository;
        private readonly int _worldServerId;

        public ServerLauncherMQListener(IOptions<OWSInstanceLauncherOptions> owsInstanceLauncherOptions, IOptions<APIPathOptions> owsAPIPathOptions, IOptions<RabbitMQOptions> rabbitMQOptions, IHttpClientFactory httpClientFactory, IZoneServerProcessesRepository zoneServerProcessesRepository,
            IOWSInstanceLauncherDataRepository owsInstanceLauncherDataRepository)
        {
            _owsInstanceLauncherOptions = owsInstanceLauncherOptions;
            _owsAPIPathOptions = owsAPIPathOptions;
            _rabbitMQOptions = rabbitMQOptions;
            _httpClientFactory = httpClientFactory;
            _zoneServerProcessesRepository = zoneServerProcessesRepository;
            _owsInstanceLauncherDataRepository = owsInstanceLauncherDataRepository;
            _customerGUID = new Guid(owsInstanceLauncherOptions.Value.OWSAPIKey);
            _worldServerId = GetWorldServerID();

            InitRabbitMQ();
        }

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
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Attempting to Register OWS Instance Launcher with RabbitMQ ServerSpinUp Queue...");

            var factory = new ConnectionFactory()
            {
                HostName = _rabbitMQOptions.Value.RabbitMQHostName,
                Port = _rabbitMQOptions.Value.RabbitMQPort,
                UserName = _rabbitMQOptions.Value.RabbitMQUserName,
                Password = _rabbitMQOptions.Value.RabbitMQPassword,
                DispatchConsumersAsync = true
            };

            // create connection  
            connection = factory.CreateConnection();

            // create channel for server spin up  
            serverSpinUpChannel = connection.CreateModel();

            serverSpinUpQueueNameGUID = Guid.NewGuid();

            serverSpinUpChannel.ExchangeDeclare(exchange: "ows.serverspinup",
                            type: "direct",
                            durable: false,
                            autoDelete: false);

            serverSpinUpChannel.QueueDeclare(queue: serverSpinUpQueueNameGUID.ToString(),
                                         durable: false,
                                         exclusive: true,
                                         autoDelete: true,
                                         arguments: null);
            serverSpinUpChannel.QueueBind(serverSpinUpQueueNameGUID.ToString(), "ows.serverspinup", String.Format("ows.serverspinup.{0}", _worldServerId));
            serverSpinUpChannel.BasicQos(0, 1, false);


            // create channel for server shut down
            serverShutDownChannel = connection.CreateModel();

            serverShutDownQueueNameGUID = Guid.NewGuid();

            serverShutDownChannel.ExchangeDeclare(exchange: "ows.servershutdown",
                            type: "direct",
                            durable: false,
                            autoDelete: false);

            serverShutDownChannel.QueueDeclare(queue: serverShutDownQueueNameGUID.ToString(),
                                         durable: false,
                                         exclusive: true,
                                         autoDelete: true,
                                         arguments: null);
            serverShutDownChannel.QueueBind(serverShutDownQueueNameGUID.ToString(), "ows.servershutdown", String.Format("ows.servershutdown.{0}", _worldServerId));
            serverShutDownChannel.BasicQos(0, 1, false);


            connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Registered OWS Instance Launcher with RabbitMQ ServerSpinUp Queue Success!");
        }

        //protected override Task ExecuteAsync(CancellationToken stoppingToken)
        public void DoWork()
        {
            //This will be null if there was a problem with initialization in the constructor
            if (_owsInstanceLauncherOptions == null)
            {
                return;
            }

            //Server Spin Up
            var serverSpinUpConsumer = new AsyncEventingBasicConsumer(serverSpinUpChannel);

            serverSpinUpConsumer.Received += (model, ea) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Server Spin Up Message Received");
                var body = ea.Body;
                MQSpinUpServerMessage serverSpinUpMessage = MQSpinUpServerMessage.Deserialize(body.ToArray());
                HandleServerSpinUpMessage(
                    serverSpinUpMessage.CustomerGUID,
                    serverSpinUpMessage.WorldServerID,
                    serverSpinUpMessage.ZoneInstanceID,
                    serverSpinUpMessage.MapName,
                    serverSpinUpMessage.Port);

                return Task.CompletedTask;
            };

            serverSpinUpConsumer.Shutdown += OnServerSpinUpConsumerShutdown;
            serverSpinUpConsumer.Registered += OnServerSpinUpConsumerRegistered;
            serverSpinUpConsumer.Unregistered += OnServerSpinUpConsumerUnregistered;
            serverSpinUpConsumer.ConsumerCancelled += OnServerSpinUpConsumerConsumerCancelled;

            serverSpinUpChannel.BasicConsume(queue: serverSpinUpQueueNameGUID.ToString(),
                                         autoAck: true,
                                         consumer: serverSpinUpConsumer);

            //Server Shut Down
            var serverShutDownConsumer = new AsyncEventingBasicConsumer(serverShutDownChannel);

            serverShutDownConsumer.Received += (model, ea) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Message Received");
                var body = ea.Body;
                MQShutDownServerMessage serverShutDownMessage = MQShutDownServerMessage.Deserialize(body.ToArray());
                HandleServerShutDownMessage(
                    serverShutDownMessage.CustomerGUID,
                    serverShutDownMessage.ZoneInstanceID
                );

                return Task.CompletedTask;
            };

            serverShutDownConsumer.Shutdown += OnServerShutDownConsumerShutdown;
            serverShutDownConsumer.Registered += OnServerShutDownConsumerRegistered;
            serverShutDownConsumer.Unregistered += OnServerShutDownConsumerUnregistered;
            serverShutDownConsumer.ConsumerCancelled += OnServerShutDownConsumerConsumerCancelled;

            serverShutDownChannel.BasicConsume(queue: serverShutDownQueueNameGUID.ToString(),
                                         autoAck: true,
                                         consumer: serverShutDownConsumer);

            //return Task.CompletedTask;
        }

        private void HandleServerSpinUpMessage(Guid customerGUID, int worldServerID, int zoneInstanceID, string mapName, int port)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Starting up {customerGUID} : {worldServerID} : {mapName} : {port}");

            //Security Check on CustomerGUID
            if (_customerGUID != customerGUID)
            {
                Console.WriteLine("HandleServerSpinUpMessage - Incoming CustomerGUID does not match OWSAPIKey in appsettings.json");
                return;
            }

            //string PathToDedicatedServer = "E:\\Program Files\\Epic Games\\UE_4.25\\Engine\\Binaries\\Win64\\UE4Editor.exe";
            //string ServerArguments = "\"C:\\OWS\\OpenWorldStarterPlugin\\OpenWorldStarter.uproject\" {0}?listen -server -log -nosteam -messaging -port={1}";

            System.Diagnostics.Process proc = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _owsInstanceLauncherOptions.Value.PathToDedicatedServer,
                    Arguments = Encoding.Default.GetString(Encoding.UTF8.GetBytes(String.Format(_owsInstanceLauncherOptions.Value.ServerArguments, mapName, port))),
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    CreateNoWindow = false
                }
            };

            proc.Start();
            proc.WaitForInputIdle();

            _zoneServerProcessesRepository.AddZoneServerProcess(new ZoneServerProcess {
                ZoneInstanceId = zoneInstanceID,
                MapName = mapName,
                Port = port,
                ProcessId = proc.Id,
                ProcessName = proc.ProcessName
            });

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{customerGUID} : {worldServerID} : {mapName} : {port} is ready for players");

            //The server has finished spinning up.  Set the status to 2.
            _ = UpdateZoneServerStatusReady(zoneInstanceID);
        }

        private void HandleServerShutDownMessage(Guid customerGUID, int zoneInstanceID)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Shutting down {customerGUID} : {zoneInstanceID}");

            //Security Check on CustomerGUID
            if (_customerGUID != customerGUID)
            {
                Console.WriteLine("HandleServerShutDownMessage - Incoming CustomerGUID does not match OWSAPIKey in appsettings.json");
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
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Shutting down all Server Instances...");
            Console.ForegroundColor = ConsoleColor.White;

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

        private int StartInstanceLauncherRequest()
        {
            var instanceManagementHttpClient = _httpClientFactory.CreateClient("OWSInstanceManagement");

            var responseMessageAsync = instanceManagementHttpClient.GetAsync("api/Instance/StartInstanceLauncher");
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

            var shutDownInstanceLauncherRequest = new StringContent(JsonConvert.SerializeObject(worldServerIDRequestPayload), Encoding.UTF8, "application/json");

            var responseMessage = await instanceManagementHttpClient.PostAsync("api/Instance/ShutDownInstanceLauncher", shutDownInstanceLauncherRequest);

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

            var setZoneInstanceStatusRequest = new StringContent(JsonConvert.SerializeObject(setZoneInstanceStatusRequestPayload), Encoding.UTF8, "application/json");

            var responseMessage = await instanceManagementHttpClient.PostAsync("api/Instance/SetZoneInstanceStatus", setZoneInstanceStatusRequest);

            return;
        }

        private Task OnServerSpinUpConsumerConsumerCancelled(object sender, ConsumerEventArgs e) { return Task.CompletedTask; }
        private Task OnServerSpinUpConsumerUnregistered(object sender, ConsumerEventArgs e) { return Task.CompletedTask; }
        private Task OnServerSpinUpConsumerRegistered(object sender, ConsumerEventArgs e) { return Task.CompletedTask; }
        private Task OnServerSpinUpConsumerShutdown(object sender, ShutdownEventArgs e) { return Task.CompletedTask; }

        private Task OnServerShutDownConsumerConsumerCancelled(object sender, ConsumerEventArgs e) { return Task.CompletedTask; }
        private Task OnServerShutDownConsumerUnregistered(object sender, ConsumerEventArgs e) { return Task.CompletedTask; }
        private Task OnServerShutDownConsumerRegistered(object sender, ConsumerEventArgs e) { return Task.CompletedTask; }
        private Task OnServerShutDownConsumerShutdown(object sender, ShutdownEventArgs e) { return Task.CompletedTask; }


        private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e) { }

        public void Dispose()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Shutting Down OWS Instance Launcher...");
            Console.ForegroundColor = ConsoleColor.White;

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
            if (connection != null)
            {
                connection.Close();
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Done!");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
