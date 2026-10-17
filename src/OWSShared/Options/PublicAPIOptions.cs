using System;
using System.Collections.Generic;
using System.Text;

namespace OWSShared.Options
{
    public class PublicAPIOptions
    {
        public const string SectionName = "OWSPublicAPIConfig";
        public double SecondsToWaitForServerSpinUp { get; set; }
        public double SecondsToWaitInBetweenSpinUpPolling { get; set; }
        public double SecondsToWaitBeforeFirstPollForSpinUp { get; set; }
        public string OWSInstanceMangementBaseAddress { get; set; }
    }
}
