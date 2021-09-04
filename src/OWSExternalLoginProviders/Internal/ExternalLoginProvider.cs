using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OWSExternalLoginProviders.Interfaces;
using OWSExternalLoginProviders.Options;
using OWSExternalLoginProviders.Responses;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OWSExternalLoginProviders.Internal
{
    public abstract class ExternalLoginProvider<TOptions> : IExternalLoginProvider where TOptions : ExternalLoginProviderOptions, new()
    {
        
        /// <summary>
        /// Gets or sets the options associated with this external login provider.
        /// </summary>
        public TOptions Options { get; private set; } = default!;
 
        /// <summary>
        /// Gets or sets the HttpClientFactory for the external login provider.
        /// </summary>
        public IHttpClientFactory HttpClientFactory { get; private set; }

        /// <summary>
        /// Gets or sets the MemoryCache for the external login provider.
        /// </summary>
        public IMemoryCache MemoryCache { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="ExternalLoginProvider{TOptions}"/>.
        /// </summary>
        /// <param name="options">The options instance.</param>
        protected ExternalLoginProvider(string ProviderName, IOptionsSnapshot<TOptions> Options, IHttpClientFactory HttpClientFactory, IMemoryCache MemoryCache)
        {
            this.Options = Options.Get(ProviderName);
            this.HttpClientFactory = HttpClientFactory;
            this.MemoryCache = MemoryCache;
        }

        /// <summary>
        /// Redirects to ExternalLoginProvider login page.
        /// </summary>
        /// <param name="state">The state token for <see href="https://docs.microsoft.com/en-us/aspnet/core/security/anti-request-forgery">Cross-Site Request Forgery (CSRF)</see></param>
        /// <returns><see cref="RedirectResult"/></returns>
        public abstract RedirectResult AuthenticationRedirect(string state);

        /// <summary>
        /// Authenticate using Password Grant Type
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="ExternalLoginProviderResponse"/></returns>
        public abstract Task<ExternalLoginProviderResponse> AuthenticatePasswordAsync(string username, string password, CancellationToken cancellationToken);

        /// <summary>
        /// Authenticate using Authorization Exchange Grant Type
        /// </summary>
        /// <param name="exchange_code">Exchange Code</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="ExternalLoginProviderResponse"/></returns>
        public abstract Task<ExternalLoginProviderResponse> AuthenticateAuthorizationExchangeAsync(string exchange_code, CancellationToken cancellationToken);

        /// <summary>
        /// Authenticate using Authorization Code Grant Type
        /// </summary>
        /// <param name="code">Authorization Code</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="ExternalLoginProviderResponse"/></returns>
        public abstract Task<ExternalLoginProviderResponse> AuthenticateAuthorizationCodeAsync(string authorization_code, CancellationToken cancellationToken);

        /// <summary>
        /// Authenticate using Device Token Grant Type
        /// </summary>
        /// <param name="token">Device Token</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="ExternalLoginProviderResponse"/></returns>
        public abstract Task<ExternalLoginProviderResponse> AuthenticateDeviceToken(string token, CancellationToken cancellationToken);

        /// <summary>
        /// Two Factor Authentication
        /// </summary>
        /// <param name="method">The Authentication Method</param>
        /// <param name="code">The Authentication Code</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="ExternalLoginProviderResponse"/></returns>
        public abstract Task<ExternalLoginProviderResponse> TwoFactorAuthentication(string method, string code, CancellationToken cancellationToken);

        /// <summary>
        /// Verify Authentication Token
        /// </summary>
        /// <param name="token">Token to be verified.</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="bool"/></returns>
        public abstract Task<bool> VerifyToken(string token, CancellationToken cancellationToken);
    }
}
