using System.Text.Json.Serialization;

namespace Robin.Extensions.Gemini.Entity;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum GeminiRole
{
    [JsonStringEnumMemberName("user")]
    User,

    [JsonStringEnumMemberName("model")]
    Model,
}
