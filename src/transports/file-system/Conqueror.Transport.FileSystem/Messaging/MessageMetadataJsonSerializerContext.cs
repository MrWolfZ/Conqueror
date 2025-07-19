namespace Conqueror.Transport.FileSystem.Messaging;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(MessageMetadata))]
[JsonSerializable(typeof(MessageResponseMetadata))]
internal sealed partial class MessageMetadataJsonSerializerContext : JsonSerializerContext;
