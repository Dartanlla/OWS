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
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(this);
            return bytes;
        }

        public static MQShutDownServerMessage Deserialize(byte[] data)
        {
            MQShutDownServerMessage messageToReturn = new MQShutDownServerMessage();

            string json = Encoding.UTF8.GetString(data);
            messageToReturn = JsonSerializer.Deserialize<MQShutDownServerMessage>(json);

            return messageToReturn;
        }
    }
}
