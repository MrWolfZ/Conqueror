using System.Text.Json;
using System.Text.Json.Serialization;

namespace Conqueror.Transport.FileSystem.Signalling;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(SignalMetadata))]
internal sealed partial class SignalMetadataJsonSerializerContext : JsonSerializerContext;
