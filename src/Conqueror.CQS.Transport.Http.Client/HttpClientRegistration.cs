using System;

namespace Conqueror.CQS.Transport.Http.Client;

internal sealed class HttpClientRegistration
{
    public Action<HttpCommandClientOptions>? CommandConfigurationAction { get; init; }

    public Action<HttpQueryClientOptions>? QueryConfigurationAction { get; init; }

    public Uri BaseAddress { get; init; } = default!;

    public Type? CommandType { get; init; }

    public Type? QueryType { get; init; }
}
