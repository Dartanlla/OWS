using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;

namespace OWSPublicAPI.Requests.Users
{
    /// <summary>
    /// CreateCharacterRequest Handler
    /// </summary>
    /// <remarks>
    /// Handles api/Users/CreateCharacter requests.
    /// </remarks>
    public class CreateCharacterRequest : IRequestHandler<CreateCharacterRequest, IActionResult>, IRequest
    {
        /// <summary>
        /// UserSessionGUID Request Paramater.
        /// </summary>
        /// <remarks>
        /// Contains the User Session GUID from the request.  This identifies the User we are modifying.
        /// </remarks>
        public Guid UserSessionGUID { get; set; }
        /// <summary>
        /// CharacterName Request Paramater.
        /// </summary>
        /// <remarks>
        /// Contains the Character Name from the request.  This is the new Character Name to create.
        /// </remarks>
        public string CharacterName { get; set; }
        /// <summary>
        /// ClassName Request Paramater.
        /// </summary>
        /// <remarks>
        /// Contains the Class Name from the request.  This sets the default values for the new Character.
        /// </remarks>
        public string ClassName { get; set; }
        /// <summary>
        /// Initial persistent character data JSON.
        /// </summary>
        /// <remarks>
        /// Contains project-specific persistent data to write atomically with character creation.
        /// </remarks>
        public string InitialPersistentData { get; set; }

        private const string NewCharacterFieldName = "bIsNewCharacter";

        private CreateCharacter Output;
        private Guid CustomerGUID;
        private IUsersRepository usersRepository;
        private IPublicAPIInputValidation publicAPIInputValidation;

        /// <summary>
        /// Set Dependencies for CreateCharacterRequest
        /// </summary>
        /// <remarks>
        /// Injects the dependencies for the CreateCharacterRequest.
        /// </remarks>
        public void SetData(IUsersRepository usersRepository, IPublicAPIInputValidation publicAPIInputValidation, IHeaderCustomerGUID customerGuid)
        {
            CustomerGUID = customerGuid.CustomerGUID;
            this.usersRepository = usersRepository;
            this.publicAPIInputValidation = publicAPIInputValidation;
        }

        /// <summary>
        /// Handles the CreateCharacterRequest
        /// </summary>
        /// <remarks>
        /// Overrides IRequestHandler Handle().
        /// </remarks>
        public async Task<IActionResult> Handle()
        {
            //Validate Character Name
            string errorMessage = publicAPIInputValidation.ValidateCharacterName(CharacterName);

            if (!String.IsNullOrEmpty(errorMessage))
            {
                CreateCharacter createCharacterErrorMessage = new CreateCharacter();
                createCharacterErrorMessage.ErrorMessage = errorMessage;
                return new OkObjectResult(createCharacterErrorMessage);
            }

            if (String.IsNullOrWhiteSpace(InitialPersistentData))
            {
                CreateCharacter createCharacterErrorMessage = new CreateCharacter();
                createCharacterErrorMessage.Success = false;
                createCharacterErrorMessage.ErrorMessage = "InitialPersistentData is required.";
                return new OkObjectResult(createCharacterErrorMessage);
            }

            // The zone server keeps only the creation-screen fields of a new character's data, so the client must not be
            // able to mark it as saved. Unreal matches JSON keys ignoring case, so every spelling of the flag goes.
            JsonObject persistentData = null;
            List<string> newFlagKeys = null;
            try
            {
                persistentData = JsonNode.Parse(InitialPersistentData) as JsonObject;
                newFlagKeys = persistentData?.Select(property => property.Key)
                    .Where(name => String.Equals(name, NewCharacterFieldName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            catch (Exception exception) when (exception is JsonException || exception is ArgumentException)
            {
                persistentData = null;
            }

            if (persistentData == null)
            {
                CreateCharacter createCharacterErrorMessage = new CreateCharacter();
                createCharacterErrorMessage.Success = false;
                createCharacterErrorMessage.ErrorMessage = "InitialPersistentData must be a JSON object.";
                return new OkObjectResult(createCharacterErrorMessage);
            }

            foreach (string newFlagKey in newFlagKeys)
            {
                persistentData.Remove(newFlagKey);
            }
            persistentData[NewCharacterFieldName] = true;
            InitialPersistentData = persistentData.ToJsonString();

            Output = await usersRepository.CreateSamsaraCharacter(CustomerGUID, UserSessionGUID, CharacterName, ClassName, InitialPersistentData);

            return new OkObjectResult(Output);
        }
    }
}
