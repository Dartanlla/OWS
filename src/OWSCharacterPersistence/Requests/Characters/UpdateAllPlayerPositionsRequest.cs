using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSCharacterPersistence.Requests.Characters
{
    public class UpdateAllPlayerPositionsRequest
    {
        public string SerializedPlayerLocationData { get; set; }
        public string MapName { get; set; }

        private Guid customerGUID;
        private ICharactersRepository charactersRepository;

        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            this.charactersRepository = charactersRepository;
            customerGUID = customerGuid.CustomerGUID;
        }

        public async Task<SuccessAndErrorMessage> Handle()
        {
            SuccessAndErrorMessage successAndErrorMessage = new SuccessAndErrorMessage();

            foreach (string PlayerDataString in SerializedPlayerLocationData.Split('|'))
            {
                string[] PlayerDataValues = PlayerDataString.Split(':');

                string PlayerName = PlayerDataValues[0];
                string sX = PlayerDataValues[1];
                string sY = PlayerDataValues[2];
                string sZ = PlayerDataValues[3];
                string sRX = PlayerDataValues[4];
                string sRY = PlayerDataValues[5];
                string sRZ = PlayerDataValues[6];

                float X = float.Parse(sX);
                float Y = float.Parse(sY);
                float Z = float.Parse(sZ);
                float RX = float.Parse(sRX);
                float RY = float.Parse(sRY);
                float RZ = float.Parse(sRZ);

                await charactersRepository.UpdatePosition(customerGUID, PlayerName, MapName, X, Y, Z, RX, RY, RZ);
            }

            successAndErrorMessage.Success = true;

            return successAndErrorMessage;
        }
    }
}
