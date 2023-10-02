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
    public class AddGuildAbilitiesRequest
    {
        private Guid _customerGUID;
        private ICharactersRepository _charactersRepository;

        private GuildToSend _guildInfo;

        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid, GuildToSend guildInfo)
        {
            _charactersRepository = charactersRepository;
            _customerGUID = customerGuid.CustomerGUID;
            _guildInfo = guildInfo;

        }

        public async Task<GuildToSend> Handle()
        {
            GuildToSend guildInformation = await _charactersRepository.AddGuildAbilities(_customerGUID, _guildInfo);

            return guildInformation;
        }
    }
}
