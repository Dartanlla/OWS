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
            //Test for empty Character Names or Character Names that are shorter than the minimum Character name Length
            if (String.IsNullOrEmpty(charName) || charName.Length < 4)
            {
                return "Please enter a valid Character Name that is at least 4 characters in length.";
            }

            //Test for Character Names that use characters other than Letters (uppercase and lowercase) and Numbers.
            Regex regex = new Regex(@"^\w+$");
            if (!regex.IsMatch(charName))
            {
                return "Please enter a Character Name that only contains letters and numbers.";
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
