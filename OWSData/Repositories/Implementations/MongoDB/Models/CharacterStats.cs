using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Repositories.Implementations.MongoDB.Models
{
    class CharacterStats
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("CustomerId")]
        public Guid CustomerId { get; set; }
        [BsonElement("CharacterName")]
        public string CharacterName { get; set; }

        [BsonElement("CharacterStatsJSONString")]
        public string CharacterStatsJSONString { get; set; }
    }
}
