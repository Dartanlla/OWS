using IdentityModel.Client;
using Microsoft.Extensions.Options;
using OWSExternalLoginProviders.Interfaces;
using OWSExternalLoginProviders.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OWSExternalLoginProviders.Internal
{
    public abstract class ExternalLoginProvider<TOptions> : IExternalLoginProvider where TOptions : ExternalLoginProviderOptions, new()
    {
        /// <summary>
        /// Gets or sets the options associated with this external login provider.
        /// </summary>
        public IOptions<TOptions> Options { get; private set; } = default!;

        /// <summary>
        /// Initializes a new instance of <see cref="ExternalLoginProvider{TOptions}"/>.
        /// </summary>
        /// <param name="options">The options instance.</param>
        protected ExternalLoginProvider(IOptions<TOptions> Options)
        {
            this.Options = Options;
        }

        /// <summary>
        /// Authenticate using Password Grant Type
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns><see cref="TokenResponse"/></returns>
        public abstract Task<TokenResponse> AuthenticatePasswordAsync(string username, string password);

        /// <summary>
        /// Authenticate using Authorization Code Grant Type
        /// </summary>
        /// <param name="code"></param>
        /// <returns><see cref="TokenResponse"/></returns>
        public abstract Task<TokenResponse> AuthenticateAuthorizationCodeAsync(string code);

        /// <summary>
        /// Authenticate using Device Token Grant Type
        /// </summary>
        /// <param name="token"></param>
        /// <returns><see cref="TokenResponse"/></returns>
        public abstract Task<TokenResponse> AuthenticateDeviceToken(string token);
    }
}
