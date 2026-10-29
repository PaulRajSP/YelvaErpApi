using System.Text.Json.Serialization;

namespace WebApplication1.Common
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AttachmentModule
    { Order }
}
