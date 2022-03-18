using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;

namespace OWSShared.Messages
{
    [Serializable]
    public class MQSpinUpServerMessage
    {
        public Guid CustomerGUID { get; set; }
        public int WorldServerID { get; set; }
        public int ZoneInstanceID { get; set; }
        public string MapName { get; set; }
        public int Port { get; set; }

        public byte[] SerialiseIntoBinary()
        {
            byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this));
            return bytes;
        }

        public static MQSpinUpServerMessage Deserialize(byte[] data)
        {
            MQSpinUpServerMessage messageToReturn = new MQSpinUpServerMessage();

            string json = Encoding.UTF8.GetString(data);
            messageToReturn = JsonSerializer.Deserialize<MQSpinUpServerMessage>(json);

            return messageToReturn;
        }
    }
}
