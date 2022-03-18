using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSShared.Messages;
using OWSShared.Options;
using RabbitMQ.Client;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSInstanceManagement.Requests.Instance
{
    public class SpinUpServerInstanceRequest : IRequestHandler<SpinUpServerInstanceRequest, IActionResult>, IRequest
    {
        public int WorldServerID { get; set; }
        public int ZoneInstanceID { get; set; }
        public string ZoneName { get; set; }
        public int Port { get; set; }

        private IOptions<RabbitMQOptions> _rabbitMQOptions;
        private SuccessAndErrorMessage Output;
        private Guid CustomerGUID;
        //private IServiceProvider ServiceProvider;
        private ICharactersRepository _charactersRepository;

        public void SetData(IOptions<RabbitMQOptions> rabbitMQOptions, ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            _rabbitMQOptions = rabbitMQOptions;
            _charactersRepository = charactersRepository;
            CustomerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
            var factory = new ConnectionFactory() 
            { 
                HostName = _rabbitMQOptions.Value.RabbitMQHostName,
                Port = _rabbitMQOptions.Value.RabbitMQPort,
                UserName = _rabbitMQOptions.Value.RabbitMQUserName, 
                Password = _rabbitMQOptions.Value.RabbitMQPassword
            };

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(exchange: "ows.serverspinup",
                        type: "direct",
                        durable: false,
                        autoDelete: false);

                    MQSpinUpServerMessage serverSpinUpMessage = new MQSpinUpServerMessage();
                    serverSpinUpMessage.CustomerGUID = CustomerGUID;
                    serverSpinUpMessage.WorldServerID = WorldServerID;
                    serverSpinUpMessage.ZoneInstanceID = ZoneInstanceID;
                    serverSpinUpMessage.MapName = ZoneName;
                    serverSpinUpMessage.Port = Port;

                    var body = serverSpinUpMessage.Serialize();

                    channel.BasicPublish(exchange: "ows.serverspinup",
                                         routingKey: String.Format("ows.serverspinup.{0}", WorldServerID),
                                         basicProperties: null,
                                         body: body);
                }
            }

            Output = new SuccessAndErrorMessage() {
                Success = true,
                ErrorMessage = ""
            };

            return new OkObjectResult(Output);
        }
    }
}
