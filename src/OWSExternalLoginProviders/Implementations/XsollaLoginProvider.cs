using Microsoft.Extensions.Options;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using OWSExternalLoginProviders.Implementations;
using OWSExternalLoginProviders.Interfaces;
using OWSExternalLoginProviders.Options;
using Microsoft.IdentityModel.Tokens;
using OWSExternalLoginProviders.Data;
using Newtonsoft.Json;

namespace OWSExternalLoginProviders.Implementations
{
    public class XsollaLoginProvider : IExternalLoginProvider
    {
        private readonly IOptions<ExternalLoginProviderOptions> externalLoginProviderOptions;
        private readonly string projectId;
        private readonly string loginUrl;
        private readonly string secretKey;

        public XsollaLoginProvider(IOptions<ExternalLoginProviderOptions> externalLoginProviderOptions)
        {
            this.externalLoginProviderOptions = externalLoginProviderOptions;
            projectId = this.externalLoginProviderOptions.Value.APIKey;
            secretKey = this.externalLoginProviderOptions.Value.SecretKey;
            loginUrl = HttpUtility.UrlEncode(this.externalLoginProviderOptions.Value.LoginURL);
        }

        public async Task<string> AuthenticateAsync(string username, string password, bool rememberMe = false)
        {
            var client = new RestClient($"https://login.xsolla.com/api/proxy/login?projectId={projectId}&login_url={loginUrl}");
            var request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/json");
            request.AddParameter("application/json", 
                $"{{\"username\":\"{username}\",\"password\":\"{password}\",\"remember_me\":false}}", 
                ParameterType.RequestBody);

            IRestResponse response = await client.ExecuteAsync(request);

            return response.Content;
        }

        public async Task<string> RegisterAsync(string username, string password, string email)
        {
            //If AutoRegister isn't set to true, then do nothing
            if (!externalLoginProviderOptions.Value.AutoRegister)
                return "";

            var client = new RestClient($"https://login.xsolla.com/api/proxy/registration?projectId={projectId}&login_url={loginUrl}");
            var request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/json");
            request.AddParameter("application/json",
                $"{{\"username\":\"{username}\",\"password\":\"{password}\",\"email\":\"{email}\"}}",
                ParameterType.RequestBody);
            IRestResponse response = await client.ExecuteAsync(request);

            return response.Content;
        }

        public bool ValidateLoginToken(string token, string username)
        {
            int tokenStart = token.IndexOf('='); //Find first = character
            int tokenEnd = token.IndexOf("\\u0026"); //& character

            if (tokenStart > 0 && tokenEnd > tokenStart)
            {
                token = token.Substring(tokenStart + 1, tokenEnd - (tokenStart + 1));

                var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

                var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

                TokenValidationParameters validationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateAudience = false,
                        ValidIssuer = "https://login.xsolla.com",
                        IssuerSigningKey = securityKey
                    };

                SecurityToken validatedToken;
                JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
                var userClaims = handler.ValidateToken(token, validationParameters, out validatedToken);

                if (userClaims != null && userClaims.Claims != null)
                {
                    var usernameClaim = userClaims.Claims.Where(x => x.Type == "username");

                    if (usernameClaim != null)
                    {
                        if (usernameClaim.FirstOrDefault().Value == username) //Valid login
                        {
                            return true; //Authenticated
                        }
                    }
                }
            }

            return false; //Not authenticated
        }

        public string GetErrorFromToken(string token)
        {
            try
            {
                XsollaLoginError xsollaLoginError = JsonConvert.DeserializeObject<XsollaLoginError>(token);

                return xsollaLoginError.error.description;
            }
            catch
            {

            }

            return "Unknown Login Error from Xsolla!";
        }
    }
}
