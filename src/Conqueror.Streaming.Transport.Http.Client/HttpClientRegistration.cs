using System;

namespace Conqueror.Streaming.Transport.Http.Client;

internal sealed class HttpClientRegistration
{
    public Action<HttpStreamClientOptions>? StreamConfigurationAction { get; init; }

    public Uri BaseAddress { get; init; } = default!;

    public Type? RequestType { get; init; }
}
