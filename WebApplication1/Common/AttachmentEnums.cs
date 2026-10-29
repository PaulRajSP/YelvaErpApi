using System.Text.Json.Serialization;

namespace AttachmentEnum
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AttachmentModule
    { Order }
}
