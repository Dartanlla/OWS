using Microsoft.AspNetCore.Mvc;
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
    public class GetServerToConnectToRequest : IRequestHandler<GetServerToConnectToRequest, IActionResult>, IRequest
    {
        public string CharacterName { get; set; }
        public string ZoneName { get; set; }
        public int PlayerGroupType { get; set; }

        private JoinMapByCharName Output;
        private Guid CustomerGUID;
        private ICharactersRepository charactersRepository;

        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            CustomerGUID = customerGuid.CustomerGUID;
            this.charactersRepository = charactersRepository;
        }

        public async Task<IActionResult> Handle()
        {
            if (String.IsNullOrEmpty(ZoneName) || ZoneName == "GETLASTZONENAME")
            {
                GetCharByCharName character = await charactersRepository.GetCharByCharName(CustomerGUID, CharacterName);

                if (character == null || String.IsNullOrEmpty(character.MapName))
                {
                    return new BadRequestResult();
                }

                ZoneName = character.MapName;
            }

            JoinMapByCharName joinMapByCharacterName = await charactersRepository.JoinMapByCharName(CustomerGUID, CharacterName, ZoneName, PlayerGroupType);

            bool readyForPlayersToConnect = false;

            if (joinMapByCharacterName.NeedToStartupMap)
            {
                var factory = new ConnectionFactory() { HostName = "localhost" };

                using (var connection = factory.CreateConnection())
                {
                    using (var channel = connection.CreateModel())
                    {
                        channel.ExchangeDeclare(exchange: "ServerSpinUp",
                            type: "direct",
                            durable: false,
                            autoDelete: false);

                        MQSpinUpServerMessage serverSpinUpMessage = new()
                        {
                            WorldServerID = joinMapByCharacterName.WorldServerID,
                            MapName = ZoneName,
                            Port = joinMapByCharacterName.WorldServerPort
                        };

                        var body = serverSpinUpMessage.Serialize();

                        channel.BasicPublish(exchange: "ServerSpinUp",
                                             routingKey: String.Format("ServerSpinUp.{0}" + joinMapByCharacterName.WorldServerID),
                                             basicProperties: null,
                                             body: body);
                    }
                }

                //Wait 5 seconds before the first CheckMapInstanceStatus to give it time to spin up
                System.Threading.Thread.Sleep(5);

                readyForPlayersToConnect = await WaitForServerReadyToConnect(joinMapByCharacterName.MapInstanceID);
            }
            else if (joinMapByCharacterName.MapInstanceID > 0 && joinMapByCharacterName.MapInstanceStatus == 1)
            {
                //CheckMapInstanceStatus every 2 seconds for up to 90 seconds
                readyForPlayersToConnect = await WaitForServerReadyToConnect(joinMapByCharacterName.MapInstanceID);
            }
            else if (joinMapByCharacterName.MapInstanceID > 0 && joinMapByCharacterName.MapInstanceStatus == 2)
            {
                //The zone server is ready to connect to
                readyForPlayersToConnect = true;
            }

            Output = joinMapByCharacterName;
            return new OkObjectResult(Output);
        }

        private async Task<bool> WaitForServerReadyToConnect(int mapInstanceID)
        {
            DateTime StartPollingTime = DateTime.Now;

            while (DateTime.Now < StartPollingTime.AddSeconds(90))
            {
                //Check Map Status
                var resultCheckMapInstanceStatus = await charactersRepository.CheckMapInstanceStatus(CustomerGUID, mapInstanceID);

                if (resultCheckMapInstanceStatus.Status == 2) //Ready to play
                {
                    /*using (RPGDataDataContext dc = new RPGDataDataContext())
                    {
                        dc.AddCharacterToMapInstanceByCharName(gCustomerGUID, id, MapInstanceID); //id = CharName
                    }*/

                    return true;
                }

                System.Threading.Thread.Sleep(2000);
            }

            return false;
        }
    }
}
