using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace OWSExternalLoginProviders.Options
{
    public abstract class ExternalLoginProviderOptions
    {
        /// <summary>
        /// Root ExternalLoginProviderConfig SectionName
        /// </summary>
        public const string SectionName = "ExternalLoginProviderConfig";

        /// <summary>
        /// External Login Provider For <see href="https://dev.epicgames.com/en-US/services">Epic Online Services</see>
        /// </summary>
        public const string EpicOnlineServices = "EpicOnlineServices";

        /// <summary>
        /// External Login Provider For <see href="https://xsolla.com/">Xsolla</see> 
        /// </summary>
        public const string Xsolla = "Xsolla";
    }
}
