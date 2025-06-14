using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Signalling.WebSockets;
using Conqueror.Transport.Http.Client.WebSockets;

namespace Conqueror.Transport.Http.Client.Signalling.WebSockets;

internal sealed class HttpWebSocketsSignalReceiverRunner(
    HttpWebSocketsSignalReceiver receiver,
    IConquerorContextAccessor conquerorContextAccessor)
{
    private readonly Lazy<HttpClient> defaultHttpClientLazy = new(() => new());

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "false positive, the source is returned to the caller")]
    public ReceiverExecutionHandle Run(CancellationToken cancellationToken)
    {
        var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var connectionTaskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        return new(
            connectionTaskCompletionSource.Task,
            Run(receiver.HandlerType, connectionTaskCompletionSource, linkedSource.Token),
            linkedSource,
            () =>
            {
                if (defaultHttpClientLazy.IsValueCreated)
                {
                    defaultHttpClientLazy.Value.Dispose();
                }
            });
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "false positive, object is disposed")]
    private async Task Run(Type? handlerType, TaskCompletionSource connectionTaskCompletionSource, CancellationToken cancellationToken)
    {
        var config = receiver.Configuration ?? throw new InvalidOperationException($"the receiver for handler type '{handlerType}' is not enabled");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ConquerorWebSocket? socket = null;
                int? statusCode = null;

                try
                {
                    (socket, statusCode) = await Connect(config, cancellationToken).ConfigureAwait(false);

                    if (statusCode is >= 400 and < 500)
                    {
                        throw new ReceiverExecutionFailedException(
                            $"failed to connect signal receiver for handler type '{handlerType}' to address '{config.Address}'; got status code {statusCode}")
                        {
                            HandlerType = handlerType,
                            SignalTransportType = new(WebSocketsTransportName, SignalTransportRole.Receiver),
                        };
                    }

                    if (socket is not null)
                    {
                        _ = connectionTaskCompletionSource.TrySetResult();

                        await foreach (var stream in socket.Read(cancellationToken).ConfigureAwait(false))
                        {
                            var (signal, contextData) =
                                await HttpWebSocketSignalProtocolV1.Read(stream, receiver.ReadSignal, cancellationToken).ConfigureAwait(false);

                            config.SignalCallback?.Invoke(signal);

                            using var conquerorContext = conquerorContextAccessor.CloneOrCreate();

                            if (contextData is not null)
                            {
                                conquerorContext.DecodeContextData(contextData);
                            }

                            await receiver.InvokeHandler(signal, cancellationToken).ConfigureAwait(false);
                        }
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    if (config.ReconnectDelayFn is not null)
                    {
                        await config.ReconnectDelayFn(
                                        socket?.CloseStatus ?? WebSocketCloseStatus.Empty,
                                        statusCode ?? 200,
                                        null,
                                        cancellationToken)
                                    .ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    // we return gracefully on cancellation
                }
                catch (IOException) when (cancellationToken.IsCancellationRequested)
                {
                    // the http stream reader might throw an IOException instead of an OperationCanceledException when
                    // the token is canceled, so we catch it here and return gracefully
                }
                catch (WebSocketException wex) when (wex.WebSocketErrorCode is not WebSocketError.UnsupportedVersion and not WebSocketError.UnsupportedProtocol)
                {
                    if (config.ReconnectDelayFn is not null)
                    {
                        await config.ReconnectDelayFn(socket?.CloseStatus ?? WebSocketCloseStatus.Empty, statusCode ?? 200, wex, cancellationToken)
                                    .ConfigureAwait(false);
                    }
                }
                catch (ReceiverExecutionFailedException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new ReceiverExecutionFailedException(
                        $"an exception occured while running receiver for signal handler type '{handlerType}'",
                        ex)
                    {
                        HandlerType = handlerType,
                        SignalTransportType = new(WebSocketsTransportName, SignalTransportRole.Receiver),
                    };
                }
                finally
                {
                    if (socket is not null)
                    {
                        await socket.DisposeAsync().ConfigureAwait(false);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            config.ExceptionCallback?.Invoke(ex);

            _ = connectionTaskCompletionSource.TrySetException(ex);

            throw;
        }
        finally
        {
            _ = connectionTaskCompletionSource.TrySetResult();
        }
    }

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "false positive, objects are returned to the caller to be disposed there")]
    private async Task<(ConquerorWebSocket? Socket, int? StatusCode)> Connect(
        HttpWebSocketsSignalReceiverConfiguration config,
        CancellationToken cancellationToken)
    {
        WebSocket? webSocket = null;

        try
        {
            var queryString = QueryStringBuilder.Create();

            foreach (var tag in receiver.Tags)
            {
                queryString = queryString.Add(QueryParameterNames.SignalWebSocketsTag, tag);
            }

            queryString = queryString.Add(QueryParameterNames.HeartbeatInterval, $"{config.HeartbeatInterval.TotalSeconds:N0}");
            queryString = queryString.Add(QueryParameterNames.HeartbeatTimeout, $"{config.HeartbeatTimeout.TotalSeconds:N0}");

            var targetUriBuilder = new UriBuilder(config.Address)
            {
                Query = queryString.Build(),
            };

            webSocket = await config.WebSocketFactory(targetUriBuilder.Uri, cancellationToken).ConfigureAwait(false);

            var socket = new ConquerorWebSocket(webSocket, config.HeartbeatInterval, config.HeartbeatTimeout);

            return (socket, null);
        }

        // special case for ASP core test host web socket client
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("Incomplete handshake, status code: "))
        {
            webSocket?.Dispose();

            var statusCode = int.Parse(ex.Message.Replace("Incomplete handshake, status code: ", string.Empty));

            return (null, statusCode);
        }
        catch
        {
            webSocket?.Dispose();

            throw;
        }
    }
}
