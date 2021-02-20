using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace OWSShared.Messages
{
    [Serializable]
    public class MQSpinUpServerMessage
    {
        public Guid CustomerGUID { get; set; }
        public int WorldServerID { get; set; }
        public int MapInstanceID { get; set; }
        public string MapName { get; set; }
        public int Port { get; set; }

        public byte[] SerialiseIntoBinary()
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(memoryStream, this);
            memoryStream.Flush();
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream.GetBuffer();
        }

        public static MQSpinUpServerMessage Deserialize(byte[] data)
        {
            MQSpinUpServerMessage messageToReturn = new MQSpinUpServerMessage();

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                try
                {
                    messageToReturn = (MQSpinUpServerMessage)binaryFormatter.Deserialize(memoryStream);
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
