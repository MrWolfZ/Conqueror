using System;
using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

// ReSharper disable once CheckNamespace
namespace Conqueror;

internal sealed class HttpSseSignalJsonSerializer<TSignal> : IHttpSseSignalSerializer<TSignal>
    where TSignal : class, IHttpSseSignal<TSignal>
{
    public void Serialize(IServiceProvider serviceProvider, TSignal signal, IBufferWriter<byte> bufferWriter)
    {
        var jsonTypeInfo = (JsonTypeInfo<TSignal>?)TSignal.HttpSseJsonSerializerContext?.GetTypeInfo(typeof(TSignal));

        if (jsonTypeInfo == null)
        {
            var jsonSerializerSettings = (JsonSerializerOptions?)serviceProvider.GetService(typeof(JsonSerializerOptions))
                                         ?? HttpJsonSerializerOptions.DefaultJsonSerializerOptions;
            jsonTypeInfo = (JsonTypeInfo<TSignal>)jsonSerializerSettings.GetTypeInfo(typeof(TSignal));
        }

        using var jsonWriter = new Utf8JsonWriter(bufferWriter);
        JsonSerializer.Serialize(jsonWriter, signal, jsonTypeInfo);
        jsonWriter.Flush();
    }

    public TSignal Deserialize(IServiceProvider serviceProvider, string serializedSignal)
    {
        var jsonTypeInfo = (JsonTypeInfo<TSignal>?)TSignal.HttpSseJsonSerializerContext?.GetTypeInfo(typeof(TSignal));

        if (jsonTypeInfo == null)
        {
            var jsonSerializerSettings = (JsonSerializerOptions?)serviceProvider.GetService(typeof(JsonSerializerOptions))
                                         ?? HttpJsonSerializerOptions.DefaultJsonSerializerOptions;
            jsonTypeInfo = (JsonTypeInfo<TSignal>)jsonSerializerSettings.GetTypeInfo(typeof(TSignal));
        }

        return JsonSerializer.Deserialize(serializedSignal, jsonTypeInfo) ?? throw new InvalidOperationException("failed to deserialize HTTP SSE signal");
    }
}
