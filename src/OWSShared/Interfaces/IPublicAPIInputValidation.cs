using System;
using System.Collections.Generic;
using System.Text;

namespace OWSShared.Interfaces
{
    public interface IPublicAPIInputValidation
    {
        //Validate the Character Name that is input by the user.  Return empty string for success or error message text to be returned to the user as a validation error.
        public string ValidateCharacterName(string charName);

        //Validate the Email that is input by the user.  Return empty string for success or error message text to be returned to the user as a validation error.
        public string ValidateEmail(string email);

        //Validate the First Name that is input by the user.  Return empty string for success or error message text to be returned to the user as a validation error.
        public string ValidateFirstName(string firstName);

        //Validate the Last Name that is input by the user.  Return empty string for success or error message text to be returned to the user as a validation error.
        public string ValidateLastName(string lastName);

        //Validate the Password that is input by the user.  Return empty string for success or error message text to be returned to the user as a validation error.
        public string ValidatePassword(string password);
    }
}
