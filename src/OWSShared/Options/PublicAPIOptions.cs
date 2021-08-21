using System;
using System.Collections.Generic;
using System.Text;

namespace OWSShared.Options
{
    public class PublicAPIOptions
    {
        public const string SectionName = "OWSPublicAPIConfig";
        public int SecondsToWaitForServerSpinUp { get; set; }
        public int SecondsToWaitInBetweenSpinUpPolling { get; set; }
        public int SecondsToWaitBeforeFirstPollForSpinUp { get; set; }
        public string OWSInstanceMangementBaseAddress { get; set; }
    }
}
