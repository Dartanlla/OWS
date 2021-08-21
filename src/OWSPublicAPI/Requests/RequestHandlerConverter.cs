using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSPublicAPI.Requests
{
    public class RequestHandlerConverter<T> : CustomCreationConverter<T>
    {
        Container container;

        public RequestHandlerConverter(Container container)
        {
            this.container = container;
        }

        public override T Create(Type objectType)
        {
            return (T)container.GetInstance(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var obj = Create(objectType);
            serializer.Populate(reader, obj);

            return obj;
        }
    }
}
