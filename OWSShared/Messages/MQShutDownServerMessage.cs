using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

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
            MemoryStream memoryStream = new MemoryStream();
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(memoryStream, this);
            memoryStream.Flush();
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream.GetBuffer();
        }

        public static MQShutDownServerMessage Deserialize(byte[] data)
        {
            MQShutDownServerMessage messageToReturn = new MQShutDownServerMessage();

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

            return messageToReturn;
        }
    }
}
