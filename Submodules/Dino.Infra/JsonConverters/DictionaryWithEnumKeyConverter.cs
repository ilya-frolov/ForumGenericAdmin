using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Dino.Infra.JsonConverters
{
    public class DictionaryWithEnumKeyConverter<T, U> : JsonConverter where T : System.Enum
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var dictionary = (Dictionary<T, U>)value;

            writer.WriteStartObject();

            foreach (KeyValuePair<T, U> pair in dictionary)
            {
                writer.WritePropertyName(Convert.ToInt32(pair.Key).ToString());
                serializer.Serialize(writer, pair.Value);
            }

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var result = new Dictionary<T, U>();
            var jObject = JObject.Load(reader);

            foreach (var x in jObject)
            {
                T key;
                if (typeof(short) == Enum.GetUnderlyingType(typeof(T)))
                {
                    key = (T)(object)short.Parse(x.Key);
                }
                else
                {
                    key = (T)(object)int.Parse(x.Key);
                }

                U value = (U)x.Value.ToObject(typeof(U));
                result.Add(key, value);
            }

            return result;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(IDictionary<T, U>) == objectType;
        }
    }
}
