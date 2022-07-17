using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OWSShared.Implementations
{
    public class DefaultPublicAPIInputValidation : IPublicAPIInputValidation
    {
        public string ValidateCharacterName(string charName)
        {
            // Test for empty Character Names or Character Names that are shorter or higher than the allowed range for Character name
            if (String.IsNullOrEmpty(charName) || charName.Length < 4 || charName.Length > 50)
            {
                return "Please enter a valid Character Name that is in range between 4 and 50 characters.";
            }

            // Test for Character Names that use characters other than Letters (uppercase and lowercase) and Numbers (A-Z & 0-9)
            Regex regex = new Regex(@"^\w+$");
            if (!regex.IsMatch(charName))
            {
                return "Please enter a Character Name that only contains letters and numbers (A-Z & 0-9).";
            }

            return "";
        }

        public string ValidateEmail(string email)
        {
            throw new NotImplementedException();
        }

        public string ValidateFirstName(string firstName)
        {
            throw new NotImplementedException();
        }

        public string ValidateLastName(string lastName)
        {
            throw new NotImplementedException();
        }

        public string ValidatePassword(string password)
        {
            throw new NotImplementedException();
        }
    }
}
