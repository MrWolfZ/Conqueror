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
    // TODO: retry/reconnect on error
    public async Task Run(Type handlerType, CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;

        try
        {
            response = await Connect(handlerType, cancellationToken).ConfigureAwait(false);

            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            var parser = SseParser.Create(responseStream, receiver.ParseItem);

            await foreach (var item in parser.EnumerateAsync(cancellationToken).ConfigureAwait(false))
            {
                using var conquerorContext = conquerorContextAccessor.CloneOrCreate();

                // TODO: set context data from envelope
                await receiver.InvokeHandler(item.Data.Signal, cancellationToken).ConfigureAwait(false);
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
        }
    }

    private async Task<HttpResponseMessage> Connect(Type handlerType, CancellationToken cancellationToken)
    {
        var config = receiver.Configuration ?? throw new InvalidOperationException($"the receiver for handler type '{handlerType}' is not enabled");
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

            // TODO: retry with back-off, unless the status code is 4XX
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpSseSignalReceiverRunFailedException(
                    $"failed to connect signal receiver for handler type '{handlerType}' to address '{config.Address}'; got status code {response.StatusCode}")
                {
                    HandlerType = handlerType,
                };
            }

            var contentType = response.Content.Headers.TryGetValues("Content-Type", out var ct) && ct.FirstOrDefault() is { } cs ? cs : null;

            if (contentType != "text/event-stream")
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
