using System;
using System.Collections.Generic;
using System.Text;

namespace OWSExternalLoginProviders.Data
{
    public class XsollaLoginError
    {
        public XsollaLoginErrorError error { get; set; }
    }

    public class XsollaLoginErrorError
    {
        public string code { get; set; }
        public string description { get; set; }
    }
}
