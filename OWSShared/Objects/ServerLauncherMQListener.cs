using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OWSData.Models;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSShared.Messages;
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
        private Guid queueNameGUID;
        private Guid customerGUID;

        private readonly IOptions<OWSInstanceLauncherOptions> _OWSInstanceLauncherOptions;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IZoneServerProcessesRepository _zoneServerProcessesRepository;

        //string _PathToDedicatedServer;
        //string _ServerArguments;

        public ServerLauncherMQListener(IOptions<OWSInstanceLauncherOptions> OWSInstanceLauncherOptions, IHttpClientFactory httpClientFactory, IZoneServerProcessesRepository zoneServerProcessesRepository)
        {
            //_PathToDedicatedServer = PathToDedicatedServer;
            //_ServerArguments = ServerArguments;

            _OWSInstanceLauncherOptions = OWSInstanceLauncherOptions;
            _httpClientFactory = httpClientFactory;
            _zoneServerProcessesRepository = zoneServerProcessesRepository;

            InitRabbitMQ();
        }

        private void InitRabbitMQ()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Attempting to Register OWS Instance Launcher with RabbitMQ ServerSpinUp Queue...");

            var factory = new ConnectionFactory() { HostName = "localhost", DispatchConsumersAsync = true };

            // create connection  
            connection = factory.CreateConnection();

            // create channel for server spin up  
            serverSpinUpChannel = connection.CreateModel();

            queueNameGUID = Guid.NewGuid();

            serverSpinUpChannel.ExchangeDeclare(exchange: "ows.serverspinup",
                            type: "direct",
                            durable: false,
                            autoDelete: false);

            serverSpinUpChannel.QueueDeclare(queue: queueNameGUID.ToString(),
                                         durable: false,
                                         exclusive: true,
                                         autoDelete: true,
                                         arguments: null);
            serverSpinUpChannel.QueueBind(queueNameGUID.ToString(), "ows.serverspinup", "ows.serverspinup.86");
            serverSpinUpChannel.BasicQos(0, 1, false);


            //create channel for server shut down



            connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Registered OWS Instance Launcher with RabbitMQ ServerSpinUp Queue Success!");
        }

        //protected override Task ExecuteAsync(CancellationToken stoppingToken)
        public void DoWork()
        {
            //stoppingToken.ThrowIfCancellationRequested();

            var consumer = new AsyncEventingBasicConsumer(serverSpinUpChannel);

            consumer.Received += (model, ea) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Message Received");
                var body = ea.Body;
                MQSpinUpServerMessage serverSpinUpMessage = MQSpinUpServerMessage.Deserialize(body.ToArray());
                HandleServerSpinUpMessage(
                    serverSpinUpMessage.CustomerGUID,
                    serverSpinUpMessage.WorldServerID,
                    serverSpinUpMessage.MapInstanceID,
                    serverSpinUpMessage.MapName,
                    serverSpinUpMessage.Port);

                return Task.CompletedTask;
            };

            consumer.Shutdown += OnConsumerShutdown;
            consumer.Registered += OnConsumerRegistered;
            consumer.Unregistered += OnConsumerUnregistered;
            consumer.ConsumerCancelled += OnConsumerConsumerCancelled;

            serverSpinUpChannel.BasicConsume(queue: queueNameGUID.ToString(),
                                         autoAck: true,
                                         consumer: consumer);

            //return Task.CompletedTask;
        }

        private void HandleServerSpinUpMessage(Guid customerGUID, int worldServerID, int zoneInstanceID, string mapName, int port)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Starting up {customerGUID} : {worldServerID} : {mapName} : {port}");

            this.customerGUID = customerGUID;

            //string PathToDedicatedServer = "E:\\Program Files\\Epic Games\\UE_4.25\\Engine\\Binaries\\Win64\\UE4Editor.exe";
            //string ServerArguments = "\"C:\\OWS\\OpenWorldStarterPlugin\\OpenWorldStarter.uproject\" {0}?listen -server -log -nosteam -messaging -port={1}";

            System.Diagnostics.Process proc = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _OWSInstanceLauncherOptions.Value.PathToDedicatedServer,
                    Arguments = Encoding.Default.GetString(Encoding.UTF8.GetBytes(String.Format(_OWSInstanceLauncherOptions.Value.ServerArguments, mapName, port))),
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
            Console.WriteLine("{customerGUID} : {worldServerID} : {mapName} : {port} is ready for players");

            //The server has finished spinning up.  Set the status to 2.
            _ = UpdateZoneServerStatusReady(zoneInstanceID);
        }

        private void HandleServerShutDownMessage(Guid customerGUID, int zoneInstanceID)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Shutting down {customerGUID} : {zoneInstanceID}");

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

        private async Task UpdateZoneServerStatusReady(int zoneInstanceID)
        {
            var instanceManagementHttpClient = _httpClientFactory.CreateClient("OWSInstanceManagement");

            instanceManagementHttpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            instanceManagementHttpClient.DefaultRequestHeaders.Add("X-CustomerGUID", customerGUID.ToString());

            var setZoneInstanceStatusRequestPayload = new { 
                request = new SetZoneInstanceStatusRequestPayload
                {
                    MapInstanceID = zoneInstanceID,
                    InstanceStatus = 2 //Ready
                }
            };

            var setZoneInstanceStatusRequest = new StringContent(JsonConvert.SerializeObject(setZoneInstanceStatusRequestPayload), Encoding.UTF8, "application/json");

            var responseMessage = await instanceManagementHttpClient.PostAsync("api/Instance/SetZoneInstanceStatus", setZoneInstanceStatusRequest);

            return;
        }

        private Task OnConsumerConsumerCancelled(object sender, ConsumerEventArgs e) { return Task.CompletedTask; }
        private Task OnConsumerUnregistered(object sender, ConsumerEventArgs e) { return Task.CompletedTask; }
        private Task OnConsumerRegistered(object sender, ConsumerEventArgs e) { return Task.CompletedTask; }
        private Task OnConsumerShutdown(object sender, ShutdownEventArgs e) { return Task.CompletedTask; }
        private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e) { }

        /*
        public override void Dispose()
        {
            serverSpinUpChannel.Close();
            connection.Close();
            base.Dispose();
        }
        */
    }
}
