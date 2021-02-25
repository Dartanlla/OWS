using System;
using System.Collections.Generic;
using System.Text;

namespace OWSShared.Options
{
    public class APIPathOptions
    {
        public const string SectionName = "OWSAPIPathConfig";
        public string InternalRabbitMQServerHostName { get; set; }
        public string InternalRabbitMQServerPort { get; set; }
        public string InternalPublicApiURL { get; set; }
        public string InternalInstanceManagementApiURL { get; set; }
        public string InternalCharacterPersistenceApiURL { get; set; }
    }
}
