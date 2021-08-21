using System;
using System.Collections.Generic;
using System.Text;

namespace OWSExternalLoginProviders.Options
{
    public class ExternalLoginProviderOptions
    {
        public const string SectionName = "ExternalLoginProviderConfig";
        public string APIKey { get; set; }
        public string SecretKey { get; set; }
        public string LoginURL { get; set; }
        public bool AutoRegister { get; set; }
    }
}
