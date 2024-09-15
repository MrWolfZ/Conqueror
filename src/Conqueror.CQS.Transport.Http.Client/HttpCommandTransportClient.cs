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

internal sealed class HttpCommandTransportClient(
    ResolvedHttpClientOptions options,
    IConquerorContextAccessor conquerorContextAccessor)
    : ICommandTransportClient
{
    public string TransportTypeName => HttpConstants.TransportName;

    public ResolvedHttpClientOptions Options { get; } = options;

    public async Task<TResponse> Send<TCommand, TResponse>(TCommand command,
                                                                     IServiceProvider serviceProvider,
                                                                     CancellationToken cancellationToken)
        where TCommand : class
    {
        var attribute = typeof(TCommand).GetCustomAttribute<HttpCommandAttribute>()!;

        using var content = JsonContent.Create(command, null, Options.JsonSerializerOptions);

        var path = Options.CommandPathConvention?.GetCommandPath(typeof(TCommand), attribute) ?? DefaultHttpCommandPathConvention.Instance.GetCommandPath(typeof(TCommand), attribute);

        using var message = new HttpRequestMessage();
        message.Method = HttpMethod.Post;
        message.RequestUri = new(Options.BaseAddress, path);
        message.Content = content;

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

    private void ReadResponseHeaders(HttpHeaders headers)
    {
        if (conquerorContextAccessor.ConquerorContext is not { } ctx)
        {
            return;
        }

        if (headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var values))
        {
            ctx.DecodeContextData(values);
        }
    }
}
