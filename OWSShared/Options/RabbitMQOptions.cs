using System;
using System.Collections.Generic;
using System.Text;

namespace OWSShared.Options
{
    public class RabbitMQOptions
    {
        public const string SectionName = "RabbitMQOptions";
        public string RabbitMQHostName { get; set; }
        public int RabbitMQPort { get; set; }
        public string RabbitMQUserName { get; set; }
        public string RabbitMQPassword { get; set; }
    }
}
