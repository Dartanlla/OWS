using GuildServiceApp.Protos;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using PartyServiceApp.Protos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSGuild.Requests.Guild
{
    public class GetInitialGuildSettingsRequest
    {
        private Guid _customerGUID;
        private ICharactersRepository _charactersRepository;

        private string _charName;

        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid, string charName)
        {
            _charactersRepository = charactersRepository;
            _customerGUID = customerGuid.CustomerGUID;
            _charName = charName;
        }

        public async Task<GuildToSend> Handle()
        {
            GuildToSend guildInformation = await _charactersRepository.GetInitialGuildSettings(_customerGUID, _charName);

            return guildInformation;
        }
    }
}
