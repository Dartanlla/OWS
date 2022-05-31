using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OWSCharacterPersistence.Requests.Abilities;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSCharacterPersistence.Controllers
{
    /// <summary>
    /// Ability related API calls.
    /// </summary>
    /// <remarks>
    /// Contains ability related API calls that are only accessible internally.
    [Route("api/[controller]")]
    [ApiController]
    public class AbilitiesController : Controller
    {
        private readonly ICharactersRepository _charactersRepository;
        private readonly IHeaderCustomerGUID _customerGuid;

        /// <summary>
        /// Constructor for Ability related API calls.
        /// </summary>
        /// <remarks>
        /// All dependencies are injected.
        /// </remarks>
        public AbilitiesController(ICharactersRepository charactersRepository,
            IHeaderCustomerGUID customerGuid)
        {
            _charactersRepository = charactersRepository;
            _customerGuid = customerGuid;
        }

        /// <summary>
        /// OnActionExecuting override
        /// </summary>
        /// <remarks>
        /// Checks for an empty IHeaderCustomerGUID.
        /// </remarks>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (_customerGuid.CustomerGUID == Guid.Empty)
            {
                context.Result = new UnauthorizedResult();
            }
        }

        /*
        [HttpPost]
        [Route("GetAbilities")]
        [Produces(typeof(GetAbilities))]
        public async Task<IActionResult> GetAbilities([FromBody] GetAbilitiesRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }
        */

        /// <summary>
        /// Add Ability To Character
        /// </summary>
        /// <remarks>
        /// Adds an Ability to a Character and also sets the initial values of Ability Level and the per instance Custom JSON
        /// </remarks>
        /// <param name="request">
        /// <b>AbilityName</b> - This is the name of the ability to add to the character.<br/>
        /// <b>AbilityLevel</b> - This is a number representing the Ability Level of the ability to add.  If you need more per instance customization, use the Custom JSON field.<br/>
        /// <b>CharacterName</b> - This is the name of the character to add the ability to.<br/>
        /// <b>CharHasAbilitiesCustomJSON</b> - This field is used to store Custom JSON for the specific instance of this Ability.  If you have a system where each ability on a character has some kind of custom variation, then this is where to store that variation data.  In a system where an ability operates the same on every player, this field would not be used.  Don't store Ability Level in this field, as there is already a field for that.  If you need to store Custom JSON for ALL instances of an ability, use the Custom JSON on the Ability itself.
        /// </param>
        [HttpPost]
        [Route("AddAbilityToCharacter")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> AddAbilityToCharacter([FromBody] AddAbilityToCharacterRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        /// <summary>
        /// Get Character Abilities
        /// </summary>
        /// <remarks>
        /// Gets a List of the Abilities on the Character specified with CharacterName
        /// </remarks>
        /// <param name="request">
        /// <b>CharacterName</b> - This is the name of the character to get abilities for.
        /// </param>
        [HttpPost]
        [Route("GetCharacterAbilities")]
        [Produces(typeof(IEnumerable<GetCharacterAbilities>))]
        public async Task<IActionResult> GetCharacterAbilities([FromBody] GetCharacterAbilitiesRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        /// <summary>
        /// Get Abilities
        /// </summary>
        /// <remarks>
        /// Gets a List of all Abilities
        /// </remarks>
        [HttpGet]
        [Route("GetAbilities")]
        [Produces(typeof(IEnumerable<Abilities>))]
        public async Task<IEnumerable<Abilities>> GetAbilities()
        {
            GetAbilitiesRequest request = new GetAbilitiesRequest();
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        /// <summary>
        /// Get Ability Bars
        /// </summary>
        /// <remarks>
        /// Gets a List of Ability Bars for the Character specified with CharacterName
        /// </remarks>
        /// <param name="request">
        /// <b>CharacterName</b> - This is the name of the character to get ability bars for.
        /// </param>
        [HttpPost]
        [Route("GetAbilityBars")]
        [Produces(typeof(IEnumerable<GetAbilityBars>))]
        public async Task<IActionResult> GetAbilityBars([FromBody] GetAbilityBarsRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        /// <summary>
        /// Get Ability Bars and Abilities
        /// </summary>
        /// <remarks>
        /// Gets a List of Ability Bars and the Abilities on those Bars for the Character specified with CharacterName
        /// </remarks>
        /// <param name="request">
        /// <b>CharacterName</b> - This is the name of the character to get abilities for.
        /// </param>
        [HttpPost]
        [Route("GetAbilityBarsAndAbilities")]
        [Produces(typeof(IEnumerable<GetAbilityBarsAndAbilities>))]
        public async Task<IActionResult> GetAbilityBarsAndAbilities([FromBody] GetCharacterAbilitiesRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        /// <summary>
        /// Remove Ability From Character
        /// </summary>
        /// <remarks>
        /// Removes an Ability from a Character
        /// </remarks>
        /// <param name="request">
        /// <b>AbilityName</b> - This is the name of the ability to add to the character.<br/>
        /// <b>CharacterName</b> - This is the name of the character to add the ability to.
        /// </param>
        [HttpPost]
        [Route("RemoveAbilityFromCharacter")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> RemoveAbilityFromCharacter([FromBody] RemoveAbilityFromCharacterRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        /// <summary>
        /// Update Ability on Character
        /// </summary>
        /// <remarks>
        /// Adds an Ability to a Character and also sets the initial values of Ability Level and the per instance Custom JSON
        /// </remarks>
        /// <param name="request">
        /// <b>AbilityName</b> - This is the name of the ability to update on the character.<br/>
        /// <b>AbilityLevel</b> - This is a number representing the Ability Level of the ability to add.  If you need more per instance customization, use the Custom JSON field.<br/>
        /// <b>CharacterName</b> - This is the name of the character to add the ability to.<br/>
        /// <b>CharHasAbilitiesCustomJSON</b> - This field is used to store Custom JSON for the specific instance of this Ability.  If you have a system where each ability on a character has some kind of custom variation, then this is where to store that variation data.  In a system where an ability operates the same on every player, this field would not be used.  Don't store Ability Level in this field, as there is already a field for that.  If you need to store Custom JSON for ALL instances of an ability, use the Custom JSON on the Ability itself.
        /// </param>
        [HttpPost]
        [Route("UpdateAbilityOnCharacter")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> UpdateAbilityOnCharacter([FromBody] UpdateAbilityOnCharacterRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }
    }
}
