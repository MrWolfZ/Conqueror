using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Signalling;

internal sealed class FileSystemSignalJsonSerializer<TSignal> : IFileSystemSignalSerializer<TSignal>
    where TSignal : class, IFileSystemSignal<TSignal>
{
    public string FileExtension => ".json";

    public Task SerializeSignal(
        IServiceProvider serviceProvider,
        TSignal signal,
        Stream fileStream,
        CancellationToken cancellationToken)
    {
        return JsonSerializer.SerializeAsync(
            fileStream,
            signal,
            GetJsonTypeInfo(serviceProvider),
            cancellationToken);
    }

    public async Task<TSignal> DeserializeSignal(IServiceProvider serviceProvider, Stream fileStream, CancellationToken cancellationToken)
    {
        return await JsonSerializer.DeserializeAsync(fileStream, GetJsonTypeInfo(serviceProvider), cancellationToken).ConfigureAwait(false)
               ?? throw new InvalidOperationException($"failed to deserialize file stream to signal of type '{typeof(TSignal)}'");
    }

    private static JsonTypeInfo<TSignal> GetJsonTypeInfo(IServiceProvider serviceProvider)
    {
        var jsonTypeInfo = (JsonTypeInfo<TSignal>?)TSignal.FileSystemJsonSerializerContext?.GetTypeInfo(typeof(TSignal));

        if (jsonTypeInfo == null)
        {
            var jsonSerializerSettings = (JsonSerializerOptions?)serviceProvider.GetService(typeof(JsonSerializerOptions))
                                         ?? FileSystemJsonSerializerOptions.DefaultJsonSerializerOptions;
            jsonTypeInfo = (JsonTypeInfo<TSignal>)jsonSerializerSettings.GetTypeInfo(typeof(TSignal));
        }

        return jsonTypeInfo;
    }
}
