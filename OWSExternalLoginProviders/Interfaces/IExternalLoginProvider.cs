using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OWSExternalLoginProviders.Interfaces
{
    public interface IExternalLoginProvider
    {
        //Authenticate a user and return a token string
        Task<string> AuthenticateAsync(string username, string password, bool rememberMe = false);

        //Register a new user
        Task<string> RegisterAsync(string username, string password, string email);

        //Validate the Login Token returned from AuthenticateAsync
        bool ValidateLoginToken(string token, string username);

        //Get the error message from the token returned when there is an error
        string GetErrorFromToken(string token);
    }
}
