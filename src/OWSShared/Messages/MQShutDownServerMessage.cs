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

        public byte[] SerialiseIntoBinary()
        {
            /*
            MemoryStream memoryStream = new MemoryStream();
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(memoryStream, this);
            memoryStream.Flush();
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream.GetBuffer();
            */
            byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this));
            return bytes;
        }

        public static MQShutDownServerMessage Deserialize(byte[] data)
        {
            MQShutDownServerMessage messageToReturn = new MQShutDownServerMessage();

            string json = Encoding.UTF8.GetString(data);
            messageToReturn = JsonSerializer.Deserialize<MQShutDownServerMessage>(json);

            /*
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                try
                {
                    messageToReturn = (MQShutDownServerMessage)binaryFormatter.Deserialize(memoryStream);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }
            */

            return messageToReturn;
        }
    }
}
