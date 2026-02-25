using Newtonsoft.Json;

namespace Dino.CoreMvc.Admin.FieldTypePlugins
{
    [JsonObject]
    public class UrlFieldType : IConvertible
    {
        private string _BaseUrl = "https://games.v-1.co.il/game";

        [JsonProperty("BaseUrl")]
        public string BaseUrl
        {
            get
            {
                return _BaseUrl;
            }
        }
        [JsonProperty("UrlName")]
        public string? UrlName { get; set; }

        public TypeCode GetTypeCode()
        {
            return TypeCode.Object;
        }

        public bool ToBoolean(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public byte ToByte(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public char ToChar(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public DateTime ToDateTime(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public decimal ToDecimal(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public double ToDouble(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public short ToInt16(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public int ToInt32(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public long ToInt64(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public sbyte ToSByte(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public float ToSingle(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public string ToString(IFormatProvider? provider)
        {
            return JsonConvert.SerializeObject(this);
        }

        public object ToType(Type conversionType, IFormatProvider? provider)
        {
            // Handle conversion to string (most common case)
            if (conversionType == typeof(string))
            {
                return string.IsNullOrEmpty(UrlName) ? null : UrlName;
            }

            // Handle conversion to the same type
            if (conversionType == typeof(UrlFieldType))
            {
                return string.IsNullOrEmpty(UrlName) ? null : UrlName;
            }

            // Handle conversion to object
            if (conversionType == typeof(object))
            {
                return string.IsNullOrEmpty(UrlName) ? null : UrlName;
            }

            // For any other type, throw NotSupportedException with more info
            throw new InvalidCastException($"Cannot convert UrlFieldType to {conversionType.Name}");
        }

        public ushort ToUInt16(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public uint ToUInt32(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public ulong ToUInt64(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }
    }
}