using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSShared.Messages;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSInstanceManagement.Requests.Instance
{
    public class SpinUpServerInstanceRequest : IRequestHandler<SpinUpServerInstanceRequest, IActionResult>, IRequest
    {
        public int WorldServerID { get; set; }
        public int MapInstanceID { get; set; }
        public string ZoneName { get; set; }
        public int Port { get; set; }
        

        private SuccessAndErrorMessage Output;
        private Guid CustomerGUID;
        //private IServiceProvider ServiceProvider;
        private ICharactersRepository charactersRepository;

        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            CustomerGUID = customerGuid.CustomerGUID;
            this.charactersRepository = charactersRepository;
        }

        public async Task<IActionResult> Handle()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };

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
                    serverSpinUpMessage.MapInstanceID = MapInstanceID;
                    serverSpinUpMessage.MapName = ZoneName;
                    serverSpinUpMessage.Port = Port;

                    var body = serverSpinUpMessage.SerialiseIntoBinary();

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
