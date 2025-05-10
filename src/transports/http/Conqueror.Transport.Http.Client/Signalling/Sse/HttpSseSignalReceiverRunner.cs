using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.ServerSentEvents;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Conqueror.Signalling.Sse;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Transport.Http.Client.Signalling.Sse;

internal sealed class HttpSseSignalReceiverRunner<THandler>(IServiceProvider serviceProvider) : IHttpSseSignalReceiverRunner
    where THandler : class, IHttpSseSignalHandler
{
    // TODO: add test for clean shutdown
    public async Task Run(CancellationToken cancellationToken)
    {
        var invokers = serviceProvider.GetRequiredService<ISignalHandlerRegistry>()
                                      .GetReceiverHandlerInvokers<IHttpSseSignalHandlerTypesInjector>()
                                      .Where(i => i.HandlerType == typeof(THandler))
                                      .ToList();

        // TODO: add test for mixed servers
        var configurations = invokers.Select(i => i.TypesInjector.Create(new ConfigurationInjectable(serviceProvider, i)))
                                     .OfType<IRunConfiguration>()
                                     .ToList();

        var configurationByEventType = configurations.ToDictionary(c => c.EventType);

        var configurationsByAddress = configurations.GroupBy(c => (c.Address, c.HttpClient))
                                                    .ToList();

        using var defaultHttpClient = new HttpClient();

        try
        {
            foreach (var g in configurationsByAddress)
            {
                var (address, client) = g.Key;
                var httpClient = client ?? defaultHttpClient;

                // TODO: add test for duplicate event type
                var eventTypes = g.Select(c => c.EventType).Distinct().ToList();
                var configuration = g.First();

                var queryString = HttpUtility.ParseQueryString(string.Empty);

                foreach (var eventType in eventTypes)
                {
                    queryString.Add("signalTypes", eventType);
                }

                var targetUriBuilder = new UriBuilder(address)
                {
                    Query = $"?{queryString}",
                };

                using var request = new HttpRequestMessage(new("GET"), targetUriBuilder.Uri);
                configuration.ConfigureHeaders?.Invoke(request.Headers);

                var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                                               .ConfigureAwait(false);

                response = response.EnsureSuccessStatusCode();

                var contentType = response.Content.Headers.TryGetValues("Content-Type", out var ct) && ct.FirstOrDefault() is { } cs ? cs : null;

                // TODO: add test
                if (contentType != "text/event-stream")
                {
                    throw new InvalidOperationException($"the server at '{address}' does not return a valid SSE response");
                }

                var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

                var parser = SseParser.Create<HttpSseSignalEnvelope>(
                    responseStream,
                    (type, data) =>
                    {
                        if (configurationByEventType.TryGetValue(type, out var c))
                        {
                            return c.ParseItem(in data);
                        }

                        throw new InvalidOperationException($"the server at '{address}' returned an unknown event type '{type}'");
                    });

                await foreach (var item in parser.EnumerateAsync(cancellationToken).ConfigureAwait(false))
                {
                    var c = configurationByEventType.GetValueOrDefault(item.EventType);

                    Debug.Assert(c is not null, "unknown event types should have been handled during parsing");

                    using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().CloneOrCreate();

                    await c.InvokeHandler(item.Data.Signal, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // we return gracefully on cancellation
        }
    }

    private sealed class ConfigurationInjectable(
        IServiceProvider serviceProvider,
        ISignalReceiverHandlerInvoker invoker)
        : IHttpSseSignalTypesInjectable<IRunConfiguration?>
    {
        IRunConfiguration? IHttpSseSignalTypesInjectable<IRunConfiguration?>.WithInjectedTypes<TSignal, TIHandler, TH>()
        {
            var receiver = new HttpSseSignalReceiver<TSignal>(serviceProvider);
            TH.ConfigureHttpSseReceiver(receiver);

            return receiver.Configuration is { } c ? new RunConfiguration<TSignal>(serviceProvider, invoker, c) : null;
        }
    }

    private sealed class RunConfiguration<TSignal>(
        IServiceProvider serviceProvider,
        ISignalReceiverHandlerInvoker invoker,
        HttpSseSignalReceiverConfiguration configuration)
        : IRunConfiguration
        where TSignal : class, IHttpSseSignal<TSignal>
    {
        public string EventType { get; } = configuration.EventType;

        public Uri Address { get; } = configuration.Address;

        public HttpClient? HttpClient { get; } = configuration.HttpClient;

        public Action<HttpRequestHeaders>? ConfigureHeaders { get; } = configuration.ConfigureHeaders;

        public HttpSseSignalEnvelope ParseItem(in ReadOnlySpan<byte> bytes)
        {
            var content = Encoding.UTF8.GetString(bytes);

            var signal = TSignal.HttpSseSignalSerializer.Deserialize(serviceProvider, content);

            // TODO: read context
            return new(signal, null);
        }

        public Task InvokeHandler(object signal, CancellationToken cancellationToken)
        {
            return invoker.Invoke(
                (TSignal)signal,
                serviceProvider,
                ConquerorTransportHttpConstants.ServersSentEventsTransportName,
                cancellationToken);
        }
    }

    private interface IRunConfiguration
    {
        string EventType { get; }

        Uri Address { get; }

        HttpClient? HttpClient { get; }

        Action<HttpRequestHeaders>? ConfigureHeaders { get; }

        HttpSseSignalEnvelope ParseItem(in ReadOnlySpan<byte> bytes);

        Task InvokeHandler(object signal, CancellationToken cancellationToken);
    }
}

internal interface IHttpSseSignalReceiverRunner
{
    Task Run(CancellationToken cancellationToken);
}
