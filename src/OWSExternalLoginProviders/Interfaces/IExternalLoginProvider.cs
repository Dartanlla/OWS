using Microsoft.AspNetCore.Mvc;
using OWSExternalLoginProviders.Responses;
using System.Threading;
using System.Threading.Tasks;

namespace OWSExternalLoginProviders.Interfaces
{
    public interface IExternalLoginProvider
    {

        /// <summary>
        /// Redirects to ExternalLoginProvider login page
        /// </summary>
        /// <param name="state">The state token for <see href="https://docs.microsoft.com/en-us/aspnet/core/security/anti-request-forgery">Cross-Site Request Forgery (CSRF)</see></param>
        /// <returns><see cref="RedirectResult"/></returns>
        RedirectResult AuthenticationRedirect(string state);

        /// <summary>
        /// Authenticate using Password Grant Type
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="ExternalLoginProviderResponse"/></returns>
        Task<ExternalLoginProviderResponse> AuthenticatePasswordAsync(string username, string password, CancellationToken cancellationToken = default);

        /// <summary>
        /// Authenticate using Authorization Code Grant Type
        /// </summary>
        /// <param name="code">Authorization Code</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="ExternalLoginProviderResponse"/></returns>
        Task<ExternalLoginProviderResponse> AuthenticateAuthorizationCodeAsync(string authorization_code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Authenticate using Authorization Exchange Grant Type
        /// </summary>
        /// <param name="exchange_code">Exchange Code</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="ExternalLoginProviderResponse"/></returns>
        Task<ExternalLoginProviderResponse> AuthenticateAuthorizationExchangeAsync(string exchange_code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Authenticate using Device Token Grant Type
        /// </summary>
        /// <param name="token">Device Token</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="ExternalLoginProviderResponse"/></returns>
        Task<ExternalLoginProviderResponse> AuthenticateDeviceToken(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Two Factor Authentication
        /// </summary>
        /// <param name="method">The Authentication Method</param>
        /// <param name="code">The Authentication Code</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="ExternalLoginProviderResponse"/></returns>
        Task<ExternalLoginProviderResponse> TwoFactorAuthentication(string method, string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verify Authentication Token
        /// </summary>
        /// <param name="token">Token to be verified.</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="bool"/></returns>
        public abstract Task<bool> VerifyToken(string token, CancellationToken cancellationToken);
    }
}
