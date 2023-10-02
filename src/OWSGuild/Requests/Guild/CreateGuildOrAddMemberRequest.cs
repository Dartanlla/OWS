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
    public class CreateGuildOrAddMemberRequest
    {
        private GuildToSend _guildRequest;
        private Guid _customerGUID;
        private ICharactersRepository _charactersRepository;

        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid, GuildToSend guildRequest)
        {
            _charactersRepository = charactersRepository;
            _customerGUID = customerGuid.CustomerGUID;
            _guildRequest = guildRequest;
        }

        public async Task<GuildToSend> Handle()
        {
            GuildToSend GuildInformation = await _charactersRepository.CreateGuildOrAddMember(_customerGUID, _guildRequest);

            return GuildInformation;
        }
    }
}
