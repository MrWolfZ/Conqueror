using System;
using System.Net.Http;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore
{
    internal sealed record HttpEndpoint
    {
        public HttpEndpointType EndpointType { get; init; }

        public HttpMethod Method { get; init; } = default!;

        public string Path { get; init; } = default!;

        public string? Version { get; init; }

        public string Name { get; init; } = default!;

        public string OperationId { get; init; } = default!;

        public string ControllerName { get; init; } = default!;

        public string? ApiGroupName { get; init; }

        public Type RequestType { get; init; } = default!;

        public Type? ResponseType { get; init; }
    }

    internal enum HttpEndpointType
    {
        Command,
        Query,
    }
}
