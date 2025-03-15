using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public interface IHttpMessageSerializer<TMessage, TResponse>
    where TMessage : class, IMessage<TResponse>, IHttpMessage<TMessage, TResponse>, IMessageTypes<TMessage, TResponse>
{
    string? ContentType { get; }

    Task<(HttpContent? Content, string? Path, string? QueryString)> Serialize(IServiceProvider serviceProvider,
                                                                              TMessage message,
                                                                              CancellationToken cancellationToken);

    Task<TMessage> Deserialize(IServiceProvider serviceProvider,
                               Stream body,
                               string path,
                               IReadOnlyDictionary<string, IReadOnlyList<string?>>? query,
                               CancellationToken cancellationToken);
}

public interface IHttpResponseSerializer<TMessage, TResponse>
    where TMessage : class, IMessage<TResponse>, IHttpMessage<TMessage, TResponse>, IMessageTypes<TMessage, TResponse>
{
    string? ContentType { get; }

    Task Serialize(IServiceProvider serviceProvider, Stream body, TResponse response, CancellationToken cancellationToken);

    Task<TResponse> Deserialize(IServiceProvider serviceProvider, HttpContent content, CancellationToken cancellationToken);
}
