using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace OWSExternalLoginProviders.Responses
{

    public class ExternalLoginProviderResponse
    {

        /// <summary>
        /// Returns true or false if an error occurred.
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// The description of the error that occurred.
        /// </summary>
        public string ErrorDescription { get; set; }

        /// <summary>
        /// Returns true or false if Two Factor Authentication is required.
        /// </summary>
        public bool TwoFactorAuthenticationRequired { get; set; }

        /// <summary>
        /// Returns the required Two Factor Authentication Method.
        /// </summary>
        public string TwoFactorAuthenticationMethod { get; set; }

        /// <summary>
        /// Returns the Access Token when Authentication is successful.
        /// </summary>
        public string AccessToken { get; set; }

    }
}
