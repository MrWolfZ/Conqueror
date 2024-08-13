using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Conqueror.Streaming.Transport.Http.Common;

namespace Conqueror.Streaming.Transport.Http.Client;

internal sealed class HttpStreamingRequestHandler<TRequest, TItem> : IStreamingRequestHandler<TRequest, TItem>
    where TRequest : class
{
    private readonly HttpStreamingRequestAttribute attribute;
    private readonly IConquerorContextAccessor? conquerorContextAccessor;
    private readonly ResolvedHttpClientOptions options;

    public HttpStreamingRequestHandler(ResolvedHttpClientOptions options, IConquerorContextAccessor? conquerorContextAccessor)
    {
        this.options = options;
        this.conquerorContextAccessor = conquerorContextAccessor;
        attribute = typeof(TRequest).GetCustomAttribute<HttpStreamingRequestAttribute>()!;
    }

    public async IAsyncEnumerable<TItem> ExecuteRequest(TRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = attribute;

        // TODO: use service
        var regex = new Regex("(Stream(ing)?)?Request$");
        var uriString = $"/api/streams/{FirstCharToLowerCase(regex.Replace(typeof(TRequest).Name, string.Empty))}";

        var queryString = HttpUtility.ParseQueryString(string.Empty);

        foreach (var property in typeof(TRequest).GetProperties())
        {
            var paramName = FirstCharToLowerCase(property.Name);
            var value = property.GetValue(request);

            if (value is IEnumerable e)
            {
                foreach (var v in e)
                {
                    queryString.Add(paramName, v?.ToString());
                }
            }
            else if (value is not null)
            {
                queryString[paramName] = value.ToString();
            }
        }

        if (ContextValueFormatter.Format(conquerorContextAccessor?.ConquerorContext?.DownstreamContextData) is { } s)
        {
            queryString.Add(HttpConstants.ConquerorContextHeaderName, s);
        }

        if (queryString.HasKeys())
        {
            uriString += $"?{queryString}";
        }

        var address = new Uri(options.BaseAddress, uriString);

        using var webSocket = await options.WebSocketFactory(address, cancellationToken).ConfigureAwait(false);
        using var textWebSocket = new TextWebSocket(webSocket);
        using var textWebSocketWithHeartbeat = new TextWebSocketWithHeartbeat(textWebSocket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
        using var jsonWebSocket = new JsonWebSocket(textWebSocketWithHeartbeat, options.JsonSerializerOptions ?? new JsonSerializerOptions());
        using var clientWebSocket = new StreamingClientWebSocket<TItem>(jsonWebSocket);

        using var closingSemaphore = new SemaphoreSlim(1);

        async Task Close(CancellationToken ct)
        {
            // ReSharper disable AccessToDisposedClosure
            await closingSemaphore.WaitAsync(ct).ConfigureAwait(false);

            try
            {
                await clientWebSocket.Close(ct).ConfigureAwait(false);
            }
            finally
            {
                _ = closingSemaphore.Release();
            }

            // ReSharper enable AccessToDisposedClosure
        }

        await using var d = cancellationToken.Register(() => Close(CancellationToken.None).Wait(CancellationToken.None)).ConfigureAwait(false);

        var enumerator = clientWebSocket.Read(cancellationToken).GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                await Close(cancellationToken).ConfigureAwait(false);
                yield break;
            }

            try
            {
                _ = await clientWebSocket.RequestNextItem(cancellationToken).ConfigureAwait(false);

                if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
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

                case ErrorMessage { Message: { } } msg:
                    throw new HttpStreamingException(msg.Message);
            }
        }
    }

    [SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "lower-case is intentional")]
    private static string? FirstCharToLowerCase(string? str)
    {
        if (!string.IsNullOrEmpty(str) && char.IsUpper(str[0]))
        {
            return str.Length == 1 ? char.ToLower(str[0]).ToString() : char.ToLower(str[0]) + str[1..];
        }

        return str;
    }
}
