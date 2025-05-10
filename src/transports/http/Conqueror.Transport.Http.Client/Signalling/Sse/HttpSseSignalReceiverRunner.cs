using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.ServerSentEvents;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Conqueror.Transport.Http.Client.Signalling.Sse;

internal sealed class HttpSseSignalReceiverRunner(
    HttpSseSignalReceiver receiver,
    IConquerorContextAccessor conquerorContextAccessor)
{
    public async Task Run(Type handlerType, CancellationToken cancellationToken)
    {
        var config = receiver.Configuration ?? throw new InvalidOperationException($"the receiver for handler type '{handlerType}' is not enabled");

        HttpResponseMessage? response = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                response = await Connect(config, cancellationToken).ConfigureAwait(false);

                if ((int)response.StatusCode is >= 400 and < 500)
                {
                    throw new HttpSseSignalReceiverRunFailedException(
                        $"failed to connect signal receiver for handler type '{handlerType}' to address '{config.Address}'; got status code {response.StatusCode}")
                    {
                        HandlerType = handlerType,
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

                var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

                var parser = SseParser.Create(responseStream, receiver.ParseItem);

                await foreach (var item in parser.EnumerateAsync(cancellationToken).ConfigureAwait(false))
                {
                    using var conquerorContext = conquerorContextAccessor.CloneOrCreate();

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
            catch (HttpSseSignalReceiverRunFailedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new HttpSseSignalReceiverRunFailedException($"an exception occured while running receiver for signal handler type '{handlerType}'", ex)
                {
                    HandlerType = handlerType,
                };
            }
            finally
            {
                response?.Dispose();
                response = null;
            }
        }
    }

    private async Task<HttpResponseMessage> Connect(HttpSseSignalReceiverConfiguration config, CancellationToken cancellationToken)
    {
        using var defaultHttpClient = config.HttpClient is null ? new HttpClient() : null;

        HttpResponseMessage? response = null;

        try
        {
            var httpClient = config.HttpClient ?? defaultHttpClient;

            var queryString = HttpUtility.ParseQueryString(string.Empty);

            foreach (var eventType in receiver.EventTypes)
            {
                queryString.Add("signalTypes", eventType);
            }

            var targetUriBuilder = new UriBuilder(config.Address)
            {
                Query = $"?{queryString}",
            };

            using var request = new HttpRequestMessage(new("GET"), targetUriBuilder.Uri);
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
