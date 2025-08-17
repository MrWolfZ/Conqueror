namespace Conqueror.Streaming.Transport.Http.Client;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Common;

internal sealed class HttpStreamProducerTransportClient(
    ResolvedHttpClientOptions options,
    IConquerorContextAccessor conquerorContextAccessor
) : IStreamProducerTransportClient
{
    public ResolvedHttpClientOptions Options { get; } = options;

    [SuppressMessage(
        "Design",
        "MA0045:Do not use blocking calls in a sync method (need to make calling method async)",
        Justification = "cannot be made async"
    )]
    public async IAsyncEnumerable<TItem> ExecuteRequest<TRequest, TItem>(
        TRequest request,
        IServiceProvider serviceProvider,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
        where TRequest : class
    {
        var attribute = typeof(TRequest).GetCustomAttribute<HttpStreamAttribute>()!;

        var uriString =
            Options.PathConvention?.GetStreamPath(typeof(TRequest), attribute)
            ?? DefaultHttpStreamPathConvention.Instance.GetStreamPath(typeof(TRequest), attribute);
        var requestUri = new Uri(Options.BaseAddress, uriString);
        using var requestMessage = new HttpRequestMessage();

        SetHeaders(requestMessage.Headers);

        using var socket = await CreateSocket<TRequest, TItem>(requestUri, requestMessage.Headers, cancellationToken)
            .ConfigureAwait(false);

        using var closingSemaphore = new SemaphoreSlim(initialCount: 1);

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

        await using var d = cancellationToken
            .Register(() => Close(CancellationToken.None).Wait(CancellationToken.None))
            .ConfigureAwait(false);

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
                if (
                    !await enumerator.MoveNextAsync().ConfigureAwait(false) || cancellationToken.IsCancellationRequested
                )
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
                case StreamingMessageEnvelope<TItem> { Message: not null } env:
                    yield return env.Message;

                    break;

                case ErrorMessage msg:
                    throw new HttpStreamFailedException(msg.Message, statusCode: null);

                default:
                    // all ok
                    break;
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

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "all sockets are disposed when the client socket is disposed"
    )]
    [SuppressMessage(
        "Usage",
        "MA0099:Use Explicit enum value instead of 0",
        Justification = "there is no 0 value for the enum, but in error cases a 0 can be returned"
    )]
    private async Task<StreamingClientWebSocket<TRequest, TItem>> CreateSocket<TRequest, TItem>(
        Uri uri,
        HttpRequestHeaders headers,
        CancellationToken cancellationToken
    )
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
                if (socket.State is not WebSocketState.Open)
                {
                    socket.Dispose();

                    throw new HttpStreamFailedException(
                        $"streaming request of type {typeof(TRequest).Name} failed to open web socket connection",
                        cws.HttpStatusCode
                    );
                }

                if (cws.HttpStatusCode is not HttpStatusCode.OK and not 0)
                {
                    socket.Dispose();

                    throw new HttpStreamFailedException(
                        $"streaming request of type {typeof(TRequest).Name} failed with status code {cws.HttpStatusCode}",
                        cws.HttpStatusCode
                    );
                }

                ReadResponseHeaders(cws.HttpResponseHeaders);
            }

            textWebSocket = new TextWebSocket(socket);
            textWebSocketWithHeartbeat = new TextWebSocketWithHeartbeat(
                textWebSocket,
                TimeSpan.FromSeconds(value: 30),
                TimeSpan.FromSeconds(value: 60)
            );
            jsonWebSocket = new JsonWebSocket(
                textWebSocketWithHeartbeat,
                Options.JsonSerializerOptions ?? new JsonSerializerOptions()
            );

            return new StreamingClientWebSocket<TRequest, TItem>(jsonWebSocket);
        }
        catch (Exception ex) when (ex is not HttpStreamFailedException)
        {
            socket?.Dispose();
            textWebSocket?.Dispose();
            textWebSocketWithHeartbeat?.Dispose();
            jsonWebSocket?.Dispose();

            throw new HttpStreamFailedException(
                $"streaming request of type {typeof(TRequest).Name} failed",
                statusCode: null,
                ex
            );
        }
    }

    private void SetHeaders(HttpHeaders headers)
    {
        if (Activity.Current is null && conquerorContextAccessor.ConquerorContext?.TraceId is { } traceId)
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

        if (
            headers
                ?.FirstOrDefault(p =>
                    string.Equals(p.Key, HttpConstants.ConquerorContextHeaderName, StringComparison.OrdinalIgnoreCase)
                )
                .Value is
            { } values
        )
        {
            ctx.DecodeContextData(values);
        }
    }
}
