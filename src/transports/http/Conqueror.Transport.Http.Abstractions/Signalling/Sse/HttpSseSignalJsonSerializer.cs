using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

internal sealed class HttpSseSignalJsonSerializer<TSignal> : IHttpSseSignalSerializer<TSignal>
    where TSignal : class, IHttpSseSignal<TSignal>
{
    public Task<string> SerializeSignal(IServiceProvider serviceProvider, TSignal signal)
    {
        var jsonTypeInfo = (JsonTypeInfo<TSignal>?)TSignal.HttpSseJsonSerializerContext?.GetTypeInfo(typeof(TSignal));

        if (jsonTypeInfo == null)
        {
            var jsonSerializerSettings = (JsonSerializerOptions?)serviceProvider.GetService(typeof(JsonSerializerOptions))
                                         ?? HttpJsonSerializerOptions.DefaultJsonSerializerOptions;
            jsonTypeInfo = (JsonTypeInfo<TSignal>)jsonSerializerSettings.GetTypeInfo(typeof(TSignal));
        }

        return Task.FromResult(JsonSerializer.Serialize(signal, jsonTypeInfo));
    }

    public Task<TSignal> DeserializeSignal(IServiceProvider serviceProvider, string serializedSignal)
    {
        var jsonTypeInfo = (JsonTypeInfo<TSignal>?)TSignal.HttpSseJsonSerializerContext?.GetTypeInfo(typeof(TSignal));

        if (jsonTypeInfo == null)
        {
            var jsonSerializerSettings = (JsonSerializerOptions?)serviceProvider.GetService(typeof(JsonSerializerOptions))
                                         ?? HttpJsonSerializerOptions.DefaultJsonSerializerOptions;
            jsonTypeInfo = (JsonTypeInfo<TSignal>)jsonSerializerSettings.GetTypeInfo(typeof(TSignal));
        }

        return Task.FromResult(
            JsonSerializer.Deserialize(serializedSignal, jsonTypeInfo) ?? throw new InvalidOperationException("failed to deserialize HTTP SSE signal"));
    }
}
