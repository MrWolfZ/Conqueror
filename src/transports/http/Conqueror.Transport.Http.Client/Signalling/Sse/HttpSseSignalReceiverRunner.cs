using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.ServerSentEvents;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Transport.Http.Client.Signalling.Sse;

internal sealed class HttpSseSignalReceiverRunner(
    HttpSseSignalReceiver receiver,
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

    private async Task Run(Type? handlerType, TaskCompletionSource connectionTaskCompletionSource, CancellationToken cancellationToken)
    {
        var config = receiver.Configuration ?? throw new InvalidOperationException($"the receiver for handler type '{handlerType}' is not enabled");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpResponseMessage? response = null;

                try
                {
                    response = await Connect(config, cancellationToken).ConfigureAwait(false);

                    if ((int)response.StatusCode is >= 400 and < 500)
                    {
                        throw new ReceiverExecutionFailedException(
                            $"failed to connect signal receiver for handler type '{handlerType}' to address '{config.Address}'; got status code {response.StatusCode}")
                        {
                            HandlerType = handlerType,
                            SignalTransportType = new(ServersSentEventsTransportName, SignalTransportRole.Receiver),
                        };
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (config.ReconnectDelayFn is not null)
                        {
                            await config.ReconnectDelayFn((int)response.StatusCode, cancellationToken).ConfigureAwait(false);
                        }

                        continue;
                    }

                    _ = connectionTaskCompletionSource.TrySetResult();

                    var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

                    var parser = SseParser.Create(responseStream, receiver.ParseItem);

                    await foreach (var item in parser.EnumerateAsync(cancellationToken).ConfigureAwait(false))
                    {
                        config.SignalCallback?.Invoke(item.Data.Signal);

                        using var conquerorContext = conquerorContextAccessor.CloneOrCreate();

                        if (item.EventId is not null)
                        {
                            conquerorContext.SetSignalId(item.EventId);
                        }

                        if (item.Data.ContextData is { } s)
                        {
                            conquerorContext.DecodeContextData(s);
                        }

                        await receiver.InvokeHandler(item.Data.Signal, cancellationToken).ConfigureAwait(false);
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    if (config.ReconnectDelayFn is not null)
                    {
                        await config.ReconnectDelayFn((int)response.StatusCode, cancellationToken).ConfigureAwait(false);
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
                        SignalTransportType = new(ServersSentEventsTransportName, SignalTransportRole.Receiver),
                    };
                }
                finally
                {
                    response?.Dispose();
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

    private async Task<HttpResponseMessage> Connect(HttpSseSignalReceiverConfiguration config, CancellationToken cancellationToken)
    {
        var defaultHttpClient = config.HttpClient is null ? defaultHttpClientLazy.Value : null;

        HttpResponseMessage? response = null;

        try
        {
            var httpClient = config.HttpClient ?? defaultHttpClient;

            var queryString = QueryStringBuilder.Create();

            foreach (var eventType in receiver.EventTypes)
            {
                queryString = queryString.Add(QueryParameterNames.SignalSseEventType, eventType);
            }

            var targetUriBuilder = new UriBuilder(config.Address)
            {
                Query = queryString.Build(),
            };

            using var request = new HttpRequestMessage(new("GET"), targetUriBuilder.Uri);

            request.Version = config.HttpVersion;
            request.VersionPolicy = config.HttpVersionPolicy;
            config.ConfigureHeaders?.Invoke(request.Headers);

            response = await httpClient!.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                                        .ConfigureAwait(false);

            var contentType = response.Content.Headers.TryGetValues("Content-Type", out var ct) && ct.FirstOrDefault() is { } cs ? cs : null;

            if (response.IsSuccessStatusCode && contentType != "text/event-stream")
            {
                throw new InvalidOperationException($"the server at '{config.Address}' did not return a valid SSE response");
            }

            return response;
        }
        catch
        {
            response?.Dispose();

            throw;
        }
    }
}
