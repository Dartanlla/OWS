using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OWSExternalLoginProviders.Internal;
using OWSExternalLoginProviders.Options;
using OWSExternalLoginProviders.Responses;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;

namespace OWSExternalLoginProviders.Implementations
{

    public class EpicOnlineServicesLoginProvider : ExternalLoginProvider<EpicOnlineServicesOptions>
    {

        public EpicOnlineServicesLoginProvider(IOptionsSnapshot<EpicOnlineServicesOptions> Options, IHttpClientFactory Httpclientfactory, IMemoryCache MemoryCache) : base(ExternalLoginProviderOptions.EpicOnlineServices, Options, Httpclientfactory, MemoryCache) { }

        public override RedirectResult AuthenticationRedirect(string state)
        {
            var requestUrl = new RequestUrl("https://www.epicgames.com/id/authorize");
            string url = requestUrl.CreateAuthorizeUrl(clientId: Options.ClientId, responseType: "code", scope: "basic_profile", redirectUri: Options.RedirectUri, state: state);
            return new RedirectResult(url);
        }

        public override async Task<ExternalLoginProviderResponse> AuthenticateAuthorizationCodeAsync(string authorization_code, CancellationToken cancellationToken)
        {
            var httpClient = HttpClientFactory.CreateClient();
            httpClient.SetBasicAuthentication(Options.ClientId, Options.ClientSecret);
            var request = await httpClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
            {
                Address = "https://api.epicgames.dev/epic/oauth/v1/token",
                AuthorizationHeaderStyle = BasicAuthenticationHeaderStyle.Rfc6749,
                Code = authorization_code,
                RedirectUri = Options.RedirectUri,
                Parameters =
                {
                    { "deployment_id", Options.DeploymentId },
                    { "scope", "basic profile"}
                }
            }, cancellationToken);

            var response = new ExternalLoginProviderResponse();

            if (request.IsError)
            {
                response.IsError = true;
                response.ErrorDescription = request.ErrorDescription ?? "An Unknown Error Occurred, Failed To Process Authentication Request.";
                return response;
            }

            response.AccessToken = request.AccessToken;
            return response;
        }

        public override async Task<ExternalLoginProviderResponse> AuthenticateAuthorizationExchangeAsync(string exchange_code, CancellationToken cancellationToken)
        {
            var httpClient = HttpClientFactory.CreateClient();
            httpClient.SetBasicAuthentication(Options.ClientId, Options.ClientSecret);
            var request = await httpClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
            {
                Address = "https://api.epicgames.dev/epic/oauth/v1/token",
                AuthorizationHeaderStyle = BasicAuthenticationHeaderStyle.Rfc6749,
                GrantType = "exchange_code",
                Parameters =
                {
                    { "exchange_code", exchange_code },
                    { "deployment_id", Options.DeploymentId },
                    { "scope", "basic profile"}
                }
            }, cancellationToken);

            var response = new ExternalLoginProviderResponse();

            if (request.IsError)
            {
                response.IsError = true;
                response.ErrorDescription = request.ErrorDescription ?? "An Unknown Error Occurred, Failed To Process Authentication Request.";
            }

            return response;
        }

        public override Task<ExternalLoginProviderResponse> AuthenticateDeviceToken(string token, CancellationToken cancellationToken)
        {
            var response = new ExternalLoginProviderResponse();
            response.IsError = true;
            response.ErrorDescription = "Authentication By Device Token Grant Is Not Support By External Login Provider.";
            return Task.FromResult(response);
        }

        public override Task<ExternalLoginProviderResponse> AuthenticatePasswordAsync(string username, string password, CancellationToken cancellationToken)
        {
            var response = new ExternalLoginProviderResponse();
            response.IsError = true;
            response.ErrorDescription = "Authentication By Password Grant Is Not Support By External Login Provider.";
            return Task.FromResult(response);

            /*
            
            NOT WORKING NEEDS 2FA SOLUTION FROM EPIC.

            var httpClient = HttpClientFactory.CreateClient();
            httpClient.SetBasicAuthentication(Options.ClientId, Options.ClientSecret);
            var request = await httpClient.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = "https://api.epicgames.dev/epic/oauth/v1/token",
                AuthorizationHeaderStyle = BasicAuthenticationHeaderStyle.Rfc6749,
                UserName = username,
                Password = password,
                Scope = "basic profile",
                Parameters =
                {
                    { "deployment_id", Options.DeploymentId }
                }
            });

            var response = new ExternalLoginProviderResponse();
            string errorCode = request.Json.TryGetString("errorCode");
            if (errorCode == "errors.com.epicgames.common.two_factor_authentication.required")
            {
                JsonElement metadata;
                if (request.Json.TryGetProperty("metadata", out metadata))
                {
                    response.TwoFactorAuthenticationMethod = metadata.TryGetString("twoFactorMethod");
                };
                response.TwoFactorAuthenticationRequired = true;
                return response;
            }

            if (errorCode == "errors.com.epicgames.account.invalid_account_credentials")
            {
                response.IsError = true;
                response.ErrorDescription = "Invalid credentials";
                return response;
            }

            if (request.IsError)
            {
                response.IsError = true;
                response.ErrorDescription = result.ErrorDescription ?? "An Unknown Error Occurred, Failed To Process Login Request.";
                return response;
            }

            response.AccessToken = request.AccessToken;
            return response;
            */
        }

        public override Task<ExternalLoginProviderResponse> TwoFactorAuthentication(string method, string code, CancellationToken cancellationToken)
        {
            var response = new ExternalLoginProviderResponse();
            response.IsError = true;
            response.ErrorDescription = "Two Factor Authentication Is Not Support By External Login Provider.";
            return Task.FromResult(response);
        }

        public override async Task<bool> VerifyToken(string token, CancellationToken cancellationToken)
        {
            // Online Verification
            var httpClient = HttpClientFactory.CreateClient();
            var request = await httpClient.IntrospectTokenAsync(new TokenIntrospectionRequest
            {
                Address = "https://api.epicgames.dev/epic/oauth/v1/tokenInfo",
                Token = token
            }, cancellationToken);

            if (!request.IsError)
            {
                return request.IsActive;
            }
            else
            {
                // Offline Verification
                string epicgamesjwks;
                if (!MemoryCache.TryGetValue("epicgames.jwks", out epicgamesjwks))
                {
                    var jwks_request = await httpClient.GetAsync("https://api.epicgames.dev/epic/oauth/v1/.well-known/jwks.json", cancellationToken);
                    if (jwks_request.IsSuccessStatusCode)
                    {
                        string jwks_result = await jwks_request.Content.ReadAsStringAsync();
                        epicgamesjwks = await MemoryCache.GetOrCreateAsync("epicgames.jwks", entry =>
                        {
                            entry.AbsoluteExpiration = DateTimeOffset.Now.AddDays(1);
                            return Task.FromResult(jwks_result);
                        });
                    }
                    else
                    {
                        return false;
                    }
                }

                try
                {
                    JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                    var jwks = new JsonWebKeySet(epicgamesjwks);

                    SecurityToken validatedToken;
                    ClaimsPrincipal claimsPrincipal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidIssuer = "https://api.epicgames.dev/epic/oauth/v1",
                        IssuerSigningKey = jwks.Keys.First()
                    }, out validatedToken);

                    if (validatedToken != null && claimsPrincipal != null && claimsPrincipal.Claims != null)
                    {
                        Claim clientId = claimsPrincipal.Claims.Where(x => x.Type == "aud").FirstOrDefault();
                        Claim deploymentId = claimsPrincipal.Claims.Where(x => x.Type == "pfdid").FirstOrDefault();
                        if (clientId != null && deploymentId != null)
                        {
                            if (clientId.Value == Options.ClientId && deploymentId.Value == Options.DeploymentId)
                            {
                                return true;
                            }
                        }
                    }

                    return false;
                }
                catch (SecurityTokenValidationException stvex)
                {
                    // Token failed the validation.
                    return false;
                }
                catch (ArgumentException argex)
                {
                    // Token not well-formed or invalid
                    return false;
                }
            }
        }
    }
}
