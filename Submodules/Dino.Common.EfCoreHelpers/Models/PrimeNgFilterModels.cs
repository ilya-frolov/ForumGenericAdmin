using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dino.Common.EfCoreHelpers.Models
{
    /// <summary>
    /// Represents a single filter constraint from PrimeNG Table
    /// </summary>
    public class PrimeNgFilterConstraint
    {
        public object? Value { get; set; }
        public string? MatchMode { get; set; }
    }

    /// <summary>
    /// Represents filter metadata for a field from PrimeNG Table.
    /// PrimeNG can send filters in two formats:
    /// 1. Object with value/matchMode/operator: { value: "x", matchMode: "contains", operator: "and" }
    /// 2. Array of constraints: [{ value: "x", matchMode: "contains" }, ...]
    /// </summary>
    [JsonConverter(typeof(PrimeNgFilterMetadataConverter))]
    public class PrimeNgFilterMetadata
    {
        public object? Value { get; set; }
        public string? MatchMode { get; set; }
        public string? Operator { get; set; } // "and" or "or"
        public List<PrimeNgFilterConstraint>? Constraints { get; set; }
        
        /// <summary>
        /// Gets all constraints as a normalized list
        /// </summary>
        public List<PrimeNgFilterConstraint> GetConstraints()
        {
            if (Constraints != null && Constraints.Count > 0)
                return Constraints;
            
            // If no constraints but we have Value/MatchMode, create a single constraint
            if (Value != null || MatchMode != null)
            {
                return new List<PrimeNgFilterConstraint>
                {
                    new PrimeNgFilterConstraint { Value = Value, MatchMode = MatchMode }
                };
            }
            
            return new List<PrimeNgFilterConstraint>();
        }
    }

    /// <summary>
    /// Custom JSON converter to handle PrimeNG's flexible filter format (Newtonsoft.Json)
    /// </summary>
    public class PrimeNgFilterMetadataConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(PrimeNgFilterMetadata);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var result = new PrimeNgFilterMetadata();

            if (reader.TokenType == JsonToken.StartArray)
            {
                // PrimeNG sent an array of constraints directly (old format or simple multi-filter)
                var array = JArray.Load(reader);
                result.Constraints = array.ToObject<List<PrimeNgFilterConstraint>>(serializer);
                result.Operator = "and"; // Default to AND for multiple constraints
            }
            else if (reader.TokenType == JsonToken.StartObject)
            {
                // PrimeNG sent an object - parse it
                var obj = JObject.Load(reader);
                
                if (obj["value"] != null)
                    result.Value = obj["value"]?.ToObject<object>(serializer);
                
                if (obj["matchMode"] != null)
                    result.MatchMode = obj["matchMode"]?.ToString();
                
                if (obj["operator"] != null)
                    result.Operator = obj["operator"]?.ToString();
                
                if (obj["constraints"] != null)
                    result.Constraints = obj["constraints"]?.ToObject<List<PrimeNgFilterConstraint>>(serializer);
            }

            return result;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var metadata = (PrimeNgFilterMetadata)value;
            writer.WriteStartObject();
            
            if (metadata.Value != null)
            {
                writer.WritePropertyName("value");
                serializer.Serialize(writer, metadata.Value);
            }
            
            if (metadata.MatchMode != null)
            {
                writer.WritePropertyName("matchMode");
                writer.WriteValue(metadata.MatchMode);
            }
            
            if (metadata.Operator != null)
            {
                writer.WritePropertyName("operator");
                writer.WriteValue(metadata.Operator);
            }
            
            if (metadata.Constraints != null && metadata.Constraints.Count > 0)
            {
                writer.WritePropertyName("constraints");
                serializer.Serialize(writer, metadata.Constraints);
            }
            
            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// Represents sorting metadata from PrimeNG Table
    /// </summary>
    public class PrimeNgSortMetadata
    {
        public string? Field { get; set; }
        public int Order { get; set; } // 1 for asc, -1 for desc
    }

    /// <summary>
    /// Represents the complete PrimeNG table request payload
    /// </summary>
    public class PrimeNgTableRequest
    {
        public int First { get; set; } = 0;
        public int Rows { get; set; } = 25;
        public string? SortField { get; set; }
        public int? SortOrder { get; set; }
        public List<PrimeNgSortMetadata>? MultiSortMeta { get; set; }
        public Dictionary<string, PrimeNgFilterMetadata>? Filters { get; set; }
        public string? GlobalFilter { get; set; }

        public int Page => First / Rows;
        public int PageSize => Rows;
    }

    /// <summary>
    /// Represents the result of applying PrimeNG filters
    /// </summary>
    public class PrimeNgFilterResult<T>
    {
        public IQueryable<T> Query { get; set; } = null!;
        public int TotalRecords { get; set; }
    }

    /// <summary>
    /// Represents a paged result from PrimeNG table processing
    /// </summary>
    public class PrimeNgPagedResult<T> where T : class
    {
        public List<T> Items { get; set; } = new List<T>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}


