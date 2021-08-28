using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace OWSExternalLoginProviders.Options
{
    /// <summary>
    /// Epic Online Services
    /// <br/><br/>
    /// Config Format :
    /// <code>
    /// <br>"EpicOnlineServices": {</br>
    /// <br>  "ClientId": "",</br>
    /// <br>  "ClientSecret": "",</br>
    /// <br>  "DeploymentId": "",</br>
    /// <br>  "RedirectUri": ""</br>
    /// <br>}</br>
    /// </code>
    /// </summary>
    public class EpicOnlineServicesOptions : ExternalLoginProviderOptions
    {

        [Required]
        public string ClientId { get; set; }

        [Required]
        public string ClientSecret { get; set; }

        [Required]
        public string DeploymentId { get; set; }

        public string RedirectUri { get; set; }
        
    }
}
