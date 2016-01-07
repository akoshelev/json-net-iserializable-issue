using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ISerializableIssue
{
    class CustomIntConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is int)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("$");
                writer.WriteValue(GetString(value));
                writer.WriteEndObject();
            }
            else
            {
                serializer.Serialize(writer, value);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var val = serializer.Deserialize(reader);
            return Deserialize(val, objectType, serializer);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(int);
        }

        private static object Deserialize(object deserializedValue, Type type, JsonSerializer serializer)
        {
            var j = deserializedValue as JObject;

            if (j != null)
            {
                if (j["$"] != null)
                {
                    var value = j["$"].Value<string>();
                    return GetValue(value);
                }

                //The JObject is not of our concern, let Json.NET deserialize it.
                return j.ToObject(type, serializer);
            }

            return deserializedValue;
        }

        private static object GetValue(string v)
        {
            var t = v.Substring(0, 1);
            v = v.Substring(1);

            if (t == "I")
                return int.Parse(v, NumberFormatInfo.InvariantInfo);

            throw new NotSupportedException();
        }

        private object GetString(object value)
        {
            if (value is int)
                return "I" + ((int)value).ToString(NumberFormatInfo.InvariantInfo);

            throw new NotSupportedException();
        }
    }

    class DeserializableObject
    {
        public DeserializableObject(int value1, decimal value2, float value3)
        {
            Value1 = value1;
            Value2 = value2;
            Value3 = value3;
        }

        public int Value1 { get; set; }
        public decimal Value2 { get; set; }
        public float Value3 { get; set; }

        public DeserializableObject EmbeddedObject { get; set; }
    }

    class NonDeserializableObject
    {
        public string Value { get; set; }

        public Exception Error { get; set; }

        public NonDeserializableObject(string value, Exception error)
        {
            Error = error;
            Value = value;
        }
    }

    class Program
    {
        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new CustomIntConverter() },
            TypeNameHandling = TypeNameHandling.All,
        };

        static void Main(string[] args)
        {
            var obj1 = new DeserializableObject(-100, 13.2m, 0.003f) {EmbeddedObject = new DeserializableObject(1, 2m, 3f) };
            var obj2 = new NonDeserializableObject("1", new InvalidOperationException("some message"));

            var data1 = JsonConvert.SerializeObject(obj1, Formatting.None, _settings);
            var data2 = JsonConvert.SerializeObject(obj2, Formatting.None, _settings);

            var desObj1 = JsonConvert.DeserializeObject<DeserializableObject>(data1, _settings);
            var desObj2 = JsonConvert.DeserializeObject<NonDeserializableObject>(data2, _settings);
        }
    }
}
