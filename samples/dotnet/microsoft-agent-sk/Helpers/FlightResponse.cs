using System.ComponentModel;
using System.Text.Json.Serialization;

namespace microsoft_agent_sk.Helpers
{
    public enum FlightResponseContentType
    {
        [JsonPropertyName("text")]
        Text,

        [JsonPropertyName("adaptive-card")]
        AdaptiveCard
    }
    public class FlightResponse
    {
        [JsonPropertyName("contentType")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FlightResponseContentType ContentType { get; set; }

        [JsonPropertyName("content")]
        [Description("The content of the response, may be plain text, or JSON based adaptive card but must be a string.")]
        public string Content { get; set; }
    }
}
