﻿using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    [Serializable]
    public record Customers(
        Guid CustomerGuid,
        string CustomerName,
        string CustomerEmail,
        string CustomerPhone,
        string CustomerNotes,
        bool EnableDebugLogging,
        bool? EnableAutoLoopBack,
        bool DeveloperPaid,
        DateTime? PublisherPaidDate,
        string StripeCustomerId,
        DateTime? FreeTrialStarted,
        bool SupportUnicode
        );

    //public partial class Customers
    //{
    //    public Guid CustomerGuid { get; set; }
    //    public string CustomerName { get; set; }
    //    public string CustomerEmail { get; set; }
    //    public string CustomerPhone { get; set; }
    //    public string CustomerNotes { get; set; }
    //    public bool EnableDebugLogging { get; set; }
    //    public bool? EnableAutoLoopBack { get; set; }
    //    public bool DeveloperPaid { get; set; }
    //    public DateTime? PublisherPaidDate { get; set; }
    //    public string StripeCustomerId { get; set; }
    //    public DateTime? FreeTrialStarted { get; set; }
    //    public bool SupportUnicode { get; set; }
    //}
}
