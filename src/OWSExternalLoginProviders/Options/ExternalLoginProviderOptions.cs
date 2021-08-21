using System;
using System.Collections.Generic;
using System.Text;

namespace OWSExternalLoginProviders.Options
{
    public class ExternalLoginProviderOptions
    {

        public const string SectionName = "ExternalLoginProviderConfig";
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }

    }
}
