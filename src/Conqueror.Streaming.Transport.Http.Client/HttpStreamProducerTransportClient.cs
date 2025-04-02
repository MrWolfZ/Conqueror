using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Streaming.Transport.Http.Common;

namespace Conqueror.Streaming.Transport.Http.Client;

internal sealed class HttpStreamProducerTransportClient(
    ResolvedHttpClientOptions options,
    IConquerorContextAccessor conquerorContextAccessor)
    : IStreamProducerTransportClient
{
    public ResolvedHttpClientOptions Options { get; } = options;

    public async IAsyncEnumerable<TItem> ExecuteRequest<TRequest, TItem>(TRequest request,
                                                                         IServiceProvider serviceProvider,
                                                                         [EnumeratorCancellation] CancellationToken cancellationToken)
        where TRequest : class
    {
        var attribute = typeof(TRequest).GetCustomAttribute<HttpStreamAttribute>()!;

        var uriString = Options.PathConvention?.GetStreamPath(typeof(TRequest), attribute) ?? DefaultHttpStreamPathConvention.Instance.GetStreamPath(typeof(TRequest), attribute);
        var requestUri = new Uri(Options.BaseAddress, uriString);
        using var requestMessage = new HttpRequestMessage();

        SetHeaders(requestMessage.Headers);

        using var socket = await CreateSocket<TRequest, TItem>(requestUri, requestMessage.Headers, cancellationToken).ConfigureAwait(false);

        using var closingSemaphore = new SemaphoreSlim(1);

        async Task Close(CancellationToken ct)
        {
            // ReSharper disable AccessToDisposedClosure
            await closingSemaphore.WaitAsync(ct).ConfigureAwait(false);

            try
            {
                await socket.Close(ct).ConfigureAwait(false);
            }
            finally
            {
                _ = closingSemaphore.Release();
            }

            // ReSharper enable AccessToDisposedClosure
        }

        await using var d = cancellationToken.Register(() => Close(CancellationToken.None).Wait(CancellationToken.None)).ConfigureAwait(false);

        var enumerator = socket.Read(cancellationToken).GetAsyncEnumerator(cancellationToken);

        _ = await socket.SendInitialRequest(request, cancellationToken).ConfigureAwait(false);

        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                await Close(cancellationToken).ConfigureAwait(false);
                yield break;
            }

            try
            {
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false) || cancellationToken.IsCancellationRequested)
                {
                    await Close(cancellationToken).ConfigureAwait(false);
                    yield break;
                }
            }
            catch (OperationCanceledException)
            {
                await Close(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
            catch
            {
                await Close(cancellationToken).ConfigureAwait(false);
                throw;
            }

            switch (enumerator.Current)
            {
                case StreamingMessageEnvelope<TItem> { Message: { } } env:
                    yield return env.Message;
                    break;

                case ErrorMessage msg:
                    throw new HttpStreamFailedException(msg.Message, null);
            }

            try
            {
                _ = await socket.RequestNextItem(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await Close(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
            catch
            {
                await Close(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "all sockets are disposed when the client socket is disposed")]
    private async Task<StreamingClientWebSocket<TRequest, TItem>> CreateSocket<TRequest, TItem>(Uri uri,
                                                                                                HttpRequestHeaders headers,
                                                                                                CancellationToken cancellationToken)
        where TRequest : class
    {
        WebSocket? socket = null;
        TextWebSocket? textWebSocket = null;
        TextWebSocketWithHeartbeat? textWebSocketWithHeartbeat = null;
        JsonWebSocket? jsonWebSocket = null;

        try
        {
            socket = await Options.SocketFactory(uri, headers, cancellationToken).ConfigureAwait(false);

            if (socket is ClientWebSocket cws)
            {
                if (socket.State != WebSocketState.Open)
                {
                    socket.Dispose();
                    throw new HttpStreamFailedException($"streaming request of type {typeof(TRequest).Name} failed to open web socket connection", cws.HttpStatusCode);
                }

                if (cws.HttpStatusCode != HttpStatusCode.OK && cws.HttpStatusCode != 0)
                {
                    socket.Dispose();
                    throw new HttpStreamFailedException($"streaming request of type {typeof(TRequest).Name} failed with status code {cws.HttpStatusCode}", cws.HttpStatusCode);
                }

                ReadResponseHeaders(cws.HttpResponseHeaders);
            }

            textWebSocket = new(socket);
            textWebSocketWithHeartbeat = new(textWebSocket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
            jsonWebSocket = new(textWebSocketWithHeartbeat, Options.JsonSerializerOptions ?? new JsonSerializerOptions());
            return new(jsonWebSocket);
        }
        catch (Exception ex) when (ex is not HttpStreamFailedException)
        {
            socket?.Dispose();
            textWebSocket?.Dispose();
            textWebSocketWithHeartbeat?.Dispose();
            jsonWebSocket?.Dispose();

            throw new HttpStreamFailedException($"streaming request of type {typeof(TRequest).Name} failed", null, ex);
        }
    }

    private void SetHeaders(HttpHeaders headers)
    {
        if (Activity.Current is null && conquerorContextAccessor.ConquerorContext?.GetTraceId() is { } traceId)
        {
            headers.Add(HttpConstants.TraceParentHeaderName, TracingHelper.CreateTraceParent(traceId: traceId));
        }

        if (conquerorContextAccessor.ConquerorContext?.EncodeDownstreamContextData() is { } data)
        {
            headers.Add(HttpConstants.ConquerorContextHeaderName, data);
        }

        if (Options.Headers is { } headersFromOptions)
        {
            foreach (var (headerName, headerValues) in headersFromOptions)
            {
                headers.Add(headerName, headerValues);
            }
        }
    }

    private void ReadResponseHeaders(IReadOnlyDictionary<string, IEnumerable<string>>? headers)
    {
        if (conquerorContextAccessor.ConquerorContext is not { } ctx)
        {
            return;
        }

        if (headers?.FirstOrDefault(p => p.Key == HttpConstants.ConquerorContextHeaderName).Value is { } values)
        {
            ctx.DecodeContextData(values);
        }
    }
}
