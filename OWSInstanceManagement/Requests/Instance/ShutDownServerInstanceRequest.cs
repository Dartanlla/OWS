using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSShared.Messages;
using OWSShared.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSInstanceManagement.Requests.Instance
{
    public class ShutDownServerInstanceRequest : IRequestHandler<SpinUpServerInstanceRequest, IActionResult>, IRequest
    {
        public int WorldServerID { get; set; }
        public int ZoneInstanceID { get; set; }

        private IOptions<APIPathOptions> owsApiPathConfig;
        private SuccessAndErrorMessage Output;
        private Guid CustomerGUID;
        private IInstanceManagementRepository instanceMangementRepository;

        public void SetData(IOptions<APIPathOptions> owsApiPathConfig, IInstanceManagementRepository instanceMangementRepository, IHeaderCustomerGUID customerGuid)
        {
            this.owsApiPathConfig = owsApiPathConfig;
            this.instanceMangementRepository = instanceMangementRepository;
            CustomerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
            //Set the ZoneInstance status to shutting down


            //Send the servershutdown message to RabbitMQ
            var factory = new ConnectionFactory() { HostName = owsApiPathConfig.Value.InternalRabbitMQServerHostName };

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(exchange: "ows.servershutdown",
                        type: "direct",
                        durable: false,
                        autoDelete: false);

                    MQShutDownServerMessage serverSpinUpMessage = new MQShutDownServerMessage();
                    serverSpinUpMessage.CustomerGUID = CustomerGUID;
                    serverSpinUpMessage.ZoneInstanceID = ZoneInstanceID;
                    
                    var body = serverSpinUpMessage.SerialiseIntoBinary();

                    channel.BasicPublish(exchange: "ows.servershutdown",
                                         routingKey: String.Format("ows.servershutdown.{0}", WorldServerID),
                                         basicProperties: null,
                                         body: body);
                }
            }

            Output = new SuccessAndErrorMessage()
            {
                Success = true,
                ErrorMessage = ""
            };

            return new OkObjectResult(Output);
        }
    }
}
