using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;

namespace OWSShared.Messages
{
    [Serializable]
    public class MQShutDownServerMessage
    {
        public Guid CustomerGUID { get; set; }
        public int WorldServerID { get; set; }
        public int ZoneInstanceID { get; set; }

        public byte[] Serialize()
        {
            return JsonSerializer.SerializeToUtf8Bytes(this);
        }

        public static MQShutDownServerMessage Deserialize(byte[] data)
        {
            return JsonSerializer.Deserialize<MQShutDownServerMessage>(Encoding.UTF8.GetString(data));
        }
    }
}
