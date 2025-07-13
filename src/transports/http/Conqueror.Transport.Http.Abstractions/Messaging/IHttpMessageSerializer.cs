using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

// TODO: make this public once the API is more stable
internal interface IHttpMessageSerializer<TMessage, TResponse>
    where TMessage : class, IHttpMessage<TMessage, TResponse>
{
    string ContentType { get; }

    string? SerializeMessageToPath(IServiceProvider serviceProvider, TMessage message) => null;

    string? SerializeMessageToQuery(IServiceProvider serviceProvider, TMessage message) => null;

    Task SerializeMessageToBody(
        IServiceProvider serviceProvider,
        TMessage message,
        Stream bodyStream,
        CancellationToken cancellationToken);

    bool TryGetBodyLength(IServiceProvider serviceProvider, TMessage message, out long length)
    {
        length = 0;
        return false;
    }

    Task<TMessage> DeserializeMessage(
        IServiceProvider serviceProvider,
        Stream bodyStream,
        Encoding? encoding,
        string path,
        IEnumerable<KeyValuePair<string, IReadOnlyList<string?>>> query,
        CancellationToken cancellationToken);
}

internal interface IHttpMessageResponseSerializer<TMessage, TResponse>
    where TMessage : class, IHttpMessage<TMessage, TResponse>
{
    string ContentType { get; }

    Task SerializeResponse(
        IServiceProvider serviceProvider,
        Stream bodyStream,
        TResponse response,
        CancellationToken cancellationToken);

    Task<TResponse> DeserializeResponse(
        IServiceProvider serviceProvider,
        Stream bodyStream,
        Encoding? encoding,
        CancellationToken cancellationToken);
}
