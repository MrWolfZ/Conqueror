using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

// ReSharper disable once CheckNamespace
namespace Conqueror;

internal sealed class HttpSseSignalJsonSerializer<TSignal> : IHttpSseSignalSerializer<TSignal>
    where TSignal : class, IHttpSseSignal<TSignal>
{
    public string Serialize(IServiceProvider serviceProvider, TSignal signal)
    {
        var jsonTypeInfo = (JsonTypeInfo<TSignal>?)TSignal.HttpSseJsonSerializerContext?.GetTypeInfo(typeof(TSignal));

        if (jsonTypeInfo == null)
        {
            var jsonSerializerSettings = (JsonSerializerOptions?)serviceProvider.GetService(typeof(JsonSerializerOptions))
                                         ?? HttpJsonSerializerOptions.DefaultJsonSerializerOptions;
            jsonTypeInfo = (JsonTypeInfo<TSignal>)jsonSerializerSettings.GetTypeInfo(typeof(TSignal));
        }

        return JsonSerializer.Serialize(signal, jsonTypeInfo);
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
