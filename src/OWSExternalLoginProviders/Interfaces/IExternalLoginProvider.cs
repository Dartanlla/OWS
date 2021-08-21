using IdentityModel.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OWSExternalLoginProviders.Interfaces
{
    public interface IExternalLoginProvider
    {
        /// <summary>
        /// Authenticate using Password Grant Type
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns><see cref="TokenResponse"/></returns>
        Task<TokenResponse> AuthenticatePasswordAsync(string username, string password);

        /// <summary>
        /// Authenticate using Authorization Code Grant Type
        /// </summary>
        /// <param name="code"></param>
        /// <returns><see cref="TokenResponse"/></returns>
        Task<TokenResponse> AuthenticateAuthorizationCodeAsync(string code);

        /// <summary>
        /// Authenticate using Device Token Grant Type
        /// </summary>
        /// <param name="token"></param>
        /// <returns><see cref="TokenResponse"/></returns>
        Task<TokenResponse> AuthenticateDeviceToken(string token);

    }
}
