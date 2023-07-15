using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Common;

namespace Conqueror.CQS.Transport.Http.Client;

internal sealed class HttpCommandTransportClient : ICommandTransportClient
{
    private readonly IConquerorContextAccessor conquerorContextAccessor;

    public HttpCommandTransportClient(ResolvedHttpClientOptions options, IConquerorContextAccessor conquerorContextAccessor)
    {
        this.conquerorContextAccessor = conquerorContextAccessor;
        Options = options;
    }

    public ResolvedHttpClientOptions Options { get; }

    public async Task<TResponse> ExecuteCommand<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
        where TCommand : class
    {
        var attribute = typeof(TCommand).GetCustomAttribute<HttpCommandAttribute>()!;

        using var content = JsonContent.Create(command, null, Options.JsonSerializerOptions);

        var path = Options.CommandPathConvention?.GetCommandPath(typeof(TCommand), attribute) ?? DefaultHttpCommandPathConvention.Instance.GetCommandPath(typeof(TCommand), attribute);

        using var message = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new(Options.BaseAddress, path),
            Content = content,
        };

        SetHeaders(message.Headers);

        TracingHelper.SetTraceParentHeaderForTestClient(message.Headers, Options.HttpClient);

        try
        {
            var response = await Options.HttpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NoContent)
            {
                var responseContent = await response.BufferAndReadContent().ConfigureAwait(false);
                throw new HttpCommandFailedException($"command of type {typeof(TCommand).Name} failed with status code {response.StatusCode} and response content: {responseContent}", response);
            }

            ReadResponseHeaders(response.Headers);

            if (typeof(TResponse) == typeof(UnitCommandResponse))
            {
                return (TResponse)(object)UnitCommandResponse.Instance;
            }

            var result = await response.Content.ReadFromJsonAsync<TResponse>(Options.JsonSerializerOptions, cancellationToken).ConfigureAwait(false);
            return result!;
        }
        catch (Exception ex) when (ex is not HttpCommandFailedException)
        {
            throw new HttpCommandFailedException($"command of type {typeof(TCommand).Name} failed", null, ex);
        }
    }

    private void SetHeaders(HttpHeaders headers)
    {
        if (Activity.Current is null && conquerorContextAccessor.ConquerorContext?.TraceId is { } traceId)
        {
            headers.Add(HttpConstants.TraceParentHeaderName, TracingHelper.CreateTraceParent(traceId: traceId));
        }

        if (ConquerorContextDataFormatter.Format(conquerorContextAccessor.ConquerorContext?.DownstreamContextData) is { } downstreamData)
        {
            headers.Add(HttpConstants.ConquerorDownstreamContextHeaderName, downstreamData);
        }

        if (ConquerorContextDataFormatter.Format(conquerorContextAccessor.ConquerorContext?.ContextData) is { } data)
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

    private void ReadResponseHeaders(HttpHeaders headers)
    {
        if (conquerorContextAccessor.ConquerorContext is not { } ctx)
        {
            return;
        }

        if (headers.TryGetValues(HttpConstants.ConquerorUpstreamContextHeaderName, out var upstreamValues))
        {
            var upstreamData = ConquerorContextDataFormatter.Parse(upstreamValues);

            foreach (var (key, value) in upstreamData)
            {
                ctx.UpstreamContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
            }
        }

        if (headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var values))
        {
            var data = ConquerorContextDataFormatter.Parse(values);

            foreach (var (key, value) in data)
            {
                ctx.ContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
            }
        }
    }
}
