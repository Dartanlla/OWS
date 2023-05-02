using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using SimpleInjector;
using OWSData.Repositories.Interfaces;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSShared.Interfaces;
using OWSPublicAPI.Requests.Users;
using OWSExternalLoginProviders.Interfaces;
using OWSShared.Options;
using Microsoft.Extensions.Options;
using System.Net.Http;
using OWSData.Models.Tables;
using Microsoft.Extensions.Logging;
using OWSPublicAPI.DTOs;

namespace OWSPublicAPI.Controllers
{
    /// <summary>
    /// Public User related API calls.
    /// </summary>
    /// <remarks>
    /// Contains user related API calls that are all publicly accessible.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        private readonly ILogger _logger;
        private readonly Container _container;
        private readonly IUsersRepository _usersRepository;
        private readonly IExternalLoginProviderFactory _externalLoginProviderFactory;
        private readonly ICharactersRepository _charactersRepository;
        private readonly IPublicAPIInputValidation _publicAPIInputValidation;
        private readonly IHeaderCustomerGUID _customerGuid;
        private readonly IOptions<PublicAPIOptions> _owsGeneralConfig;
        private readonly IOptions<APIPathOptions> _owsApiPathConfig;
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// Constructor for Public User related API calls.
        /// </summary>
        /// <remarks>
        /// All dependencies are injected.
        /// </remarks>
        public UsersController(
            ILogger<UsersController> logger,
            Container container, 
            IUsersRepository usersRepository,
            IExternalLoginProviderFactory externalLoginProviderFactory,
            ICharactersRepository charactersRepository,
            IPublicAPIInputValidation publicAPIInputValidation,
            IHeaderCustomerGUID customerGuid,
            IOptions<PublicAPIOptions> owsGeneralConfig,
            IOptions<APIPathOptions> owsApiPathConfig,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _container = container;
            _usersRepository = usersRepository;
            _externalLoginProviderFactory = externalLoginProviderFactory;
            _charactersRepository = charactersRepository;
            _publicAPIInputValidation = publicAPIInputValidation;
            _customerGuid = customerGuid;
            _owsGeneralConfig = owsGeneralConfig;
            _owsApiPathConfig = owsApiPathConfig;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Creates a new Character.
        /// </summary>
        /// <remarks>
        /// Create a new Character and attach it to the User referenced by the UserSessionGUID. ClassName is the name of the Class you setup in the management console (from the Characters tab). 
        /// Classes are meant to be default values for creating characters. The purpose is to ensure that players are not able to hack their starting stats. This is needed because Characters are created from and unsecure UE client
        /// that has not connected to a UE server yet.
        /// </remarks>
        [HttpPost]
        [Route("CreateCharacter")]
        [Produces(typeof(CreateCharacter))]
        public async Task<IActionResult> CreateCharacter([FromBody] CreateCharacterRequest request)
        {
            
            request.SetData(_usersRepository, _publicAPIInputValidation, _customerGuid);
            return await request.Handle();
        }

        /// <summary>
        /// Creates a new Character using DefaultCharacterValues.
        /// </summary>
        /// <remarks>
        /// Create a new Character and attach it to the User referenced by the UserSessionGUID. DefaultSetName is the name of the DefaultCharacterValues row. 
        /// DefaultCharacterValues and DefaultCustomCharacterData are used to set the default starting values for new characters. The purpose is to ensure that players are not able to hack their starting stats. This is needed because Characters are created from and unsecure UE client
        /// that has not connected to a UE server yet.
        /// </remarks>
        [HttpPost]
        [Route("CreateCharacterUsingDefaultCharacterValues")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> CreateCharacterUsingDefaultCharacterValues([FromBody] CreateCharacterUsingDefaultCharacterValuesDTO createCharacterUsingDefaultCharacterValuesDTO)
        {
            CreateCharacterUsingDefaultCharacterValuesRequest request = 
                new CreateCharacterUsingDefaultCharacterValuesRequest(createCharacterUsingDefaultCharacterValuesDTO, _usersRepository, _charactersRepository, _publicAPIInputValidation, _customerGuid);
            return await request.Handle();
        }

        /// <summary>
        /// Get All Characters for a specified User.
        /// </summary>
        /// <remarks>
        /// Send in a UserSessionGUID to specify which User to get all Characters for.  Use GetAllCharactersWithCustomData to also get all attached custom data with the same API call.
        /// Users may have zero or more Characters.
        /// </remarks>
        [HttpPost]
        [Route("GetAllCharacters")]
        [Produces(typeof(GetAllCharacters))]
        public async Task<IActionResult> GetAllCharacters([FromBody] GetAllCharactersRequest request)
        {
            request.SetData(_usersRepository, _customerGuid);
            return await request.Handle();
        }

        /// <summary>
        /// Gets a list of Player Groups that a Character is in.
        /// </summary>
        /// <remarks>
        /// Send a UserSessionGUID, a Character's CharacterName, and a PlayerGroupTypeID to get a list of groups the player is in.  Set the PlayerGroupTypeID parameter to zero to remove the Player Group Type filter.
        /// Player Groups are persistent across player sessions and can be used to manage Party groups, Raid groups, Guilds, etc.
        /// See the PlayerGroupTypes table for a list of Player Group Types.
        /// </remarks>
        [HttpPost]
        [Route("GetPlayerGroupsCharacterIsIn")]
        [Produces(typeof(GetPlayerGroupsCharacterIsIn))]
        public async Task<IActionResult> GetPlayerGroupsCharacterIsIn([FromBody] GetPlayerGroupsCharacterIsInRequest request)
        {
            request.SetData(_usersRepository, _customerGuid);
            return await request.Handle();
        }

        /// <summary>
        /// Gets the server to connect to for a specific character or zonename.
        /// </summary>
        /// <remarks>
        /// Get the Zone Server to travel to based on the User referenced by the UserSessionGUID.  Prior to calling this API, you MUST set the Selected Character to connect with using the UserSessionSetSelectedCharacter API for the UserSessionGUID passed into this API.
        /// Send in an empty ZoneName or ZoneName set to "GETLASTZONENAME" to use the last zone the Character was on rather than specifying a ZoneName.
        /// </remarks>
        [HttpPost]
        [Route("GetServerToConnectTo")]
        [Produces(typeof(GetServerToConnectTo))]
        public async Task<IActionResult> GetServerToConnectTo([FromBody] GetServerToConnectToRequest request)
        {
            request.SetData(_owsGeneralConfig, _usersRepository, _charactersRepository, _customerGuid, _httpClientFactory);
            return await request.Handle();
        }

        /// <summary>
        /// Gets the User Session from a UserSessionGUID.
        /// </summary>
        /// <remarks>
        /// Get the User Session object (some values may be null if a selected character is not set).
        /// </remarks>
        [HttpGet]
        [Route("GetUserSession")]
        [Produces(typeof(GetUserSession))]
        public async Task<IActionResult> GetUserSession([FromQuery] GetUserSessionRequest request)
        {
            request.SetData(_usersRepository, _customerGuid);
            return await request.Handle();
        }

        /// <summary>
        /// Login and create a User Session that you can reference via a UserSessionGUID.
        /// </summary>
        /// <remarks>
        /// Login by passing an Email and Password.  See ExternalLoginAndCreateSession when using an external login provider (such as Epic Game Store or Xsolla).
        /// </remarks>
        [HttpPost]
        [Route("LoginAndCreateSession")]
        [Produces(typeof(PlayerLoginAndCreateSession))]
        public async Task<IActionResult> LoginAndCreateSession([FromBody] LoginAndCreateSessionRequest request)
        {
            request.SetData(_usersRepository, _customerGuid);
            return await request.Handle();
        }

        /// <summary>
        /// Login and create a User Session using an External Login Provider (such as Epic Game Store or Xsolla).
        /// </summary>
        [HttpPost]
        [Route("ExternalLoginAndCreateSession")]
        [Produces(typeof(PlayerLoginAndCreateSession))]
        public async Task<IActionResult> ExternalLoginAndCreateSession([FromBody] ExternalLoginAndCreateSessionRequest request)
        {
            request.SetData(_usersRepository, _externalLoginProviderFactory, _customerGuid);
            return await request.Handle();
        }

        /// <summary>
        /// Logout of a User Session
        /// </summary>
        /// <remarks>
        /// Logout of a User Session based on a UserSessionGUID.  This method is for logout from the client side of a game before connecting to a UE server.
        /// </remarks>
        [HttpPost]
        [Route("Logout")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> Logout([FromBody] LogoutDTO request)
        {
            LogoutRequest logoutRequest = new LogoutRequest(request, _usersRepository, _customerGuid);
            return await logoutRequest.Handle();
        }

        /// <summary>
        /// Set the Character that has been selected to play for a User Session (UserSessionGUID).
        /// </summary>
        /// <remarks>
        /// This method MUST be called on a User Session before calling the GetServerToConnectTo API.
        /// </remarks>
        [HttpPost]
        [Route("UserSessionSetSelectedCharacter")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<IActionResult> UserSessionSetSelectedCharacter([FromBody] UserSessionSetSelectedCharacterRequest request)
        {
            request.SetData(_usersRepository, _customerGuid);
            return await request.Handle();
        }

        /// <summary>
        /// Set the Character that has been selected to play for a User Session (UserSessionGUID) and get the complete User Session object all in one API call.
        /// </summary>
        [HttpPost]
        [Route("SetSelectedCharacterAndGetUserSession")]
        [Produces(typeof(GetUserSession))]
        public async Task<IActionResult> SetSelectedCharacterAndGetUserSession([FromBody] SetSelectedCharacterAndGetUserSessionRequest request)
        {
            request.SetData(_usersRepository, _customerGuid);
            return await request.Handle();
        }

        /// <summary>
        /// Register a new User account by sending FirstName, LastName, Email, and Password.
        /// </summary>
        /// <remarks>
        /// Register a new User and then Logs the User in and Creates a User Session.  Implement your own IPublicAPIInputValidation to specify your specific validation rules for FirstName, LastName, Email, and Password.  You can wire up the dependency injection for your custom IPublicAPIInputValidation in Startup.cs.
        /// </remarks>
        [HttpPost]
        [Route("RegisterUser")]
        [Produces(typeof(PlayerLoginAndCreateSession))]
        public async Task<PlayerLoginAndCreateSession> RegisterUser([FromBody] RegisterUserDTO requestDTO)
        {
            RegisterUserRequest request = new RegisterUserRequest(requestDTO, _usersRepository, _externalLoginProviderFactory, _customerGuid);
            return await request.Handle();
        }

        /// <summary>
        /// Remove a Character from this User (UserSessionGUID).
        /// </summary>
        /// <remarks>
        /// Removes a Character from the User.  This method permanently deletes the character and all associated data.  In the future, it might make sense to modify this to only mark the character as removed to support restoring an accidentally removed character.
        /// </remarks>
        [HttpPost]
        [Route("RemoveCharacter")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<IActionResult> RemoveCharacter([FromBody] RemoveCharacterRequest request)
        {
            request.SetData(_usersRepository, _customerGuid);
            return await request.Handle();
        }
    }
}
