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
        private readonly Container _container;
        private readonly IUsersRepository _usersRepository;
        private readonly IExternalLoginProvider _externalLoginProvider;
        private readonly ICharactersRepository _charactersRepository;
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
        public UsersController(Container container, 
            IUsersRepository usersRepository,
            IExternalLoginProvider externalLoginProvider,
            ICharactersRepository charactersRepository, 
            IHeaderCustomerGUID customerGuid,
            IOptions<PublicAPIOptions> owsGeneralConfig,
            IOptions<APIPathOptions> owsApiPathConfig,
            IHttpClientFactory httpClientFactory)
        {
            _container = container;
            _usersRepository = usersRepository;
            _externalLoginProvider = externalLoginProvider;
            _charactersRepository = charactersRepository;
            _customerGuid = customerGuid;
            _owsGeneralConfig = owsGeneralConfig;
            _owsApiPathConfig = owsApiPathConfig;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Get a list of all Users.
        /// </summary>
        /// <remarks>
        /// Get a list of all OWS Users.
        /// </remarks>
        // GET: api/Users
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        /// <summary>
        /// Get all User Information.
        /// </summary>
        /// <remarks>
        /// Get all OWS User data.
        /// </remarks>
        // GET: api/Users/5
     //   [HttpGet("{id}", Name = "Get")]
     //   public string Get(int id)
     //   {
     //       return "value";
     //   }
        [HttpGet("{id}", Name = "Get")]
        [Produces(typeof(User))]
        public async Task<IActionResult> Get(Guid id)
        {
            GetUserRequest request = new GetUserRequest();
            request.SetData(id, _usersRepository, _customerGuid);
            return await request.Handle();
        }
        /// <summary>
        /// Save a User.
        /// </summary>
        /// <remarks>
        /// Save OWS User data.
        /// </remarks>
        // PUT: api/Users/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        /// <summary>
        /// Delete a User from OWS.
        /// </summary>
        /// <remarks>
        /// Delete an OWS User permanently.
        /// </remarks>
        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        /// <summary>
        /// Creates a new Character.
        /// </summary>
        /// <remarks>
        /// Create a new Character and attach it to the User referenced by the UserSessionGUID. ClassName is the name of the Class you setup in the management console (from the Characters tab). 
        /// Classes are meant to be default values for creating characters. The purpose is to ensure that players are not able to hack their starting stats. This is needed because Characters are created from and unsecure UE4 client
        /// that has not connected to a UE4 server yet.
        /// </remarks>
        [HttpPost]
        [Route("CreateCharacter")]
        [Produces(typeof(GetServerToConnectTo))]
        public async Task<IActionResult> CreateCharacter([FromBody] CreateCharacterRequest request)
        {
            request.SetData(_usersRepository, _customerGuid);
            return await request.Handle();
        }

        /// <summary>
        /// Get All Characters for a Specific User.
        /// </summary>
        /// <remarks>
        /// Send in a UserSessionGUID to specify which User to get all Characters for.
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
        /// Get the Zone Server to travel to based on the User referenced by the UserSessionGUID. Send in an empty ZoneName or ZoneName set to "GETLASTZONENAME" to use the last zone the Character was on rather than specifying a ZoneName.
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
        [HttpGet]
        [Route("GetUserSession")]
        [Produces(typeof(GetUserSession))]
        public async Task<IActionResult> GetUserSession([FromQuery] GetUserSessionRequest request)
        {
            request.SetData(_usersRepository, _customerGuid);
            return await request.Handle();
        }

        /// <summary>
        /// Login and create a User Session.
        /// </summary>
        [HttpPost]
        [Route("LoginAndCreateSession")]
        [Produces(typeof(PlayerLoginAndCreateSession))]
        public async Task<IActionResult> LoginAndCreateSession([FromBody] LoginAndCreateSessionRequest request)
        {
            request.SetData(_usersRepository, _customerGuid);
            return await request.Handle();
        }

        /// <summary>
        /// Login and create a User Session using an External Login Provider (such as Xsolla).
        /// </summary>
        [HttpPost]
        [Route("ExternalLoginAndCreateSession")]
        [Produces(typeof(PlayerLoginAndCreateSession))]
        public async Task<IActionResult> ExternalLoginAndCreateSession([FromBody] ExternalLoginAndCreateSessionRequest request)
        {
            request.SetData(_usersRepository, _externalLoginProvider, _customerGuid);
            return await request.Handle();
        }

        /// <summary>
        /// Set the Character that has been selected to play for a User Session.
        /// </summary>
        [HttpPost]
        [Route("UserSessionSetSelectedCharacter")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<IActionResult> UserSessionSetSelectedCharacter([FromBody] UserSessionSetSelectedCharacterRequest request)
        {
            request.SetData(_usersRepository, _customerGuid);
            return await request.Handle();
        }

        /// <summary>
        /// Set the Character that has been selected to play for a User Session and Get the User Session all in one call.
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
        /// Register a new User account.
        /// </summary>
        [HttpPost]
        [Route("RegisterUser")]
        [Produces(typeof(GetUserSession))]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterUserRequest request)
        {
            request.SetData(_usersRepository, _externalLoginProvider, _customerGuid);
            return await request.Handle();
        }

    }
}
