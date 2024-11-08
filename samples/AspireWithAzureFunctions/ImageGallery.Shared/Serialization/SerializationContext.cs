using System.Text.Json.Serialization;

namespace ImageGallery.Shared.Serialization;

[JsonSerializable(typeof(UploadResult))]
public sealed partial class SerializationContext : JsonSerializerContext
{
}
