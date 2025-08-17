namespace Conqueror.Transport.Http.Client.Signalling.WebSockets;

using System.Globalization;

internal sealed class HttpWebSocketsSignalReceiverRunner(IConquerorContextAccessor conquerorContextAccessor)
    : ISignalReceiverRunner<HttpWebSocketsSignalReceiver>
{
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "false positive, the source is returned to the caller"
    )]
    public ReceiverExecutionHandle RunReceiver(
        HttpWebSocketsSignalReceiver receiver,
        CancellationToken cancellationToken
    )
    {
        var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var connectionTaskCompletionSource = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        return new ReceiverExecutionHandle(
            connectionTaskCompletionSource.Task,
            Run(receiver, connectionTaskCompletionSource, linkedSource.Token),
            linkedSource,
            onDispose: null
        );
    }

    private async Task Run(
        HttpWebSocketsSignalReceiver receiver,
        TaskCompletionSource connectionTaskCompletionSource,
        CancellationToken cancellationToken
    )
    {
        var config =
            receiver.Configuration
            ?? throw new InvalidOperationException(
                $"the receiver for handler type '{receiver.HandlerType}' is not enabled"
            );

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ConquerorWebSocket? socket = null;
                int? statusCode = null;

                try
                {
                    (socket, statusCode) = await Connect(receiver, config, cancellationToken).ConfigureAwait(false);

                    if (statusCode is >= 400 and < 500)
                    {
                        throw new SignalReceiverExecutionFailedException(
                            string.Create(
                                CultureInfo.InvariantCulture,
                                $"failed to connect signal {nameof(receiver)} for handler type '{receiver.HandlerType}' to address '{config.Address}'; got status code {statusCode}"
                            )
                        )
                        {
                            HandlerType = receiver.HandlerType,
                            SignalTransportType = new SignalTransportType(
                                WebSocketsTransportName,
                                SignalTransportRole.Receiver
                            ),
                        };
                    }

                    if (socket is not null)
                    {
                        _ = connectionTaskCompletionSource.TrySetResult();

                        await foreach (var stream in socket.Read(cancellationToken).ConfigureAwait(false))
                        {
                            var (signal, contextData) = await HttpWebSocketsSignalProtocolV1
                                .Read(stream, receiver.ReadSignal, cancellationToken)
                                .ConfigureAwait(false);

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
                        await config
                            .ReconnectDelayFn(
                                socket?.CloseStatus ?? WebSocketCloseStatus.Empty,
                                statusCode ?? 200,
                                exception: null,
                                cancellationToken
                            )
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
                catch (WebSocketException wex)
                    when (wex.WebSocketErrorCode
                            is not WebSocketError.UnsupportedVersion
                                and not WebSocketError.UnsupportedProtocol
                    )
                {
                    if (config.ReconnectDelayFn is not null)
                    {
                        await config
                            .ReconnectDelayFn(
                                socket?.CloseStatus ?? WebSocketCloseStatus.Empty,
                                statusCode ?? 200,
                                wex,
                                cancellationToken
                            )
                            .ConfigureAwait(false);
                    }
                }
                catch (SignalReceiverExecutionFailedException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new SignalReceiverExecutionFailedException(
                        $"an exception occured while running {nameof(receiver)} for signal handler type '{receiver.HandlerType}'",
                        ex
                    )
                    {
                        HandlerType = receiver.HandlerType,
                        SignalTransportType = new SignalTransportType(
                            WebSocketsTransportName,
                            SignalTransportRole.Receiver
                        ),
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
        Justification = "false positive, objects are returned to the caller to be disposed there"
    )]
    private async Task<(ConquerorWebSocket? Socket, int? StatusCode)> Connect(
        HttpWebSocketsSignalReceiver receiver,
        HttpWebSocketsSignalReceiverConfiguration config,
        CancellationToken cancellationToken
    )
    {
        WebSocket? webSocket = null;

        try
        {
            var queryString = QueryStringBuilder.Create();

            foreach (var tag in receiver.Tags)
            {
                queryString = queryString.Add(QueryParameterNames.SignalWebSocketsTag, tag);
            }

            queryString = queryString.Add(
                QueryParameterNames.HeartbeatInterval,
                string.Create(CultureInfo.InvariantCulture, $"{config.HeartbeatInterval.TotalSeconds:N0}")
            );
            queryString = queryString.Add(
                QueryParameterNames.HeartbeatTimeout,
                string.Create(CultureInfo.InvariantCulture, $"{config.HeartbeatTimeout.TotalSeconds:N0}")
            );

            var targetUriBuilder = new UriBuilder(config.Address) { Query = queryString.Build() };

            webSocket = await config.WebSocketFactory(targetUriBuilder.Uri, cancellationToken).ConfigureAwait(false);

            var socket = new ConquerorWebSocket(webSocket, config.HeartbeatInterval, config.HeartbeatTimeout);

            return (socket, null);
        }
        catch (InvalidOperationException ex) // special case for ASP core test host web socket client
            when (ex.Message.StartsWith("Incomplete handshake, status code: ", StringComparison.Ordinal))
        {
            webSocket?.Dispose();

            var statusCode = int.Parse(
                ex.Message.Replace("Incomplete handshake, status code: ", "", StringComparison.Ordinal),
                CultureInfo.InvariantCulture
            );

            return (null, statusCode);
        }
        catch
        {
            webSocket?.Dispose();

            throw;
        }
    }
}
