using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public sealed class HttpMessageQueryStringSerializer<TMessage, TResponse>(
    Func<IReadOnlyDictionary<string, IReadOnlyList<string?>>?, TMessage> fromQueryString,
    Func<TMessage, string?> toQueryString)
    : IHttpMessageSerializer<TMessage, TResponse>
    where TMessage : class, IHttpMessage<TMessage, TResponse>
{
    public string? ContentType => null;

    public Task<(HttpContent? Content, string? Path, string? QueryString)> Serialize(IServiceProvider serviceProvider, TMessage message, CancellationToken cancellationToken)
        => Task.FromResult<(HttpContent? Content, string? Path, string? QueryString)>((null, null, toQueryString(message)));

    public Task<TMessage> Deserialize(IServiceProvider serviceProvider,
                                      Stream body,
                                      string path,
                                      IReadOnlyDictionary<string, IReadOnlyList<string?>>? query, CancellationToken cancellationToken)
        => Task.FromResult(fromQueryString(query));
}
