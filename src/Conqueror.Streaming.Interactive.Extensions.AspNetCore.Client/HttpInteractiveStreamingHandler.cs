using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Conqueror.CQS.Extensions.AspNetCore.Common;
using Conqueror.Streaming.Interactive.Extensions.AspNetCore.Common;

namespace Conqueror.Streaming.Interactive.Extensions.AspNetCore.Client
{
    internal sealed class HttpInteractiveStreamingHandler<TRequest, TItem> : IInteractiveStreamingHandler<TRequest, TItem>
        where TRequest : class
    {
        private readonly HttpInteractiveStreamAttribute attribute;
        private readonly IConquerorContextAccessor? conquerorContextAccessor;
        private readonly ResolvedHttpClientOptions options;

        public HttpInteractiveStreamingHandler(ResolvedHttpClientOptions options, IConquerorContextAccessor? conquerorContextAccessor)
        {
            this.options = options;
            this.conquerorContextAccessor = conquerorContextAccessor;
            attribute = typeof(TRequest).GetCustomAttribute<HttpInteractiveStreamAttribute>()!;
        }

        public async IAsyncEnumerable<TItem> ExecuteRequest(TRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _ = attribute;

            // TODO: use service
            var regex = new Regex("(Interactive)?(Stream(ing)?)?Request$");
            var uriString = $"/api/streams/interactive/{FirstCharToLowerCase(regex.Replace(typeof(TRequest).Name, string.Empty))}";

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

            if (conquerorContextAccessor?.ConquerorContext?.Items is { Count: > 0 } contextItems)
            {
                queryString.Add(HttpConstants.ConquerorContextHeaderName, ContextValueFormatter.Format(contextItems));
            }

            if (queryString.HasKeys())
            {
                uriString += $"?{queryString}";
            }

            var address = new Uri(options.BaseAddress, uriString);

            using var webSocket = await options.WebSocketFactory(address, cancellationToken);
            using var textWebSocket = new TextWebSocket(webSocket);
            using var textWebSocketWithHeartbeat = new TextWebSocketWithHeartbeat(textWebSocket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
            using var jsonWebSocket = new JsonWebSocket(textWebSocketWithHeartbeat, options.JsonSerializerOptions ?? new JsonSerializerOptions());
            using var clientWebSocket = new InteractiveStreamingClientWebSocket<TItem>(jsonWebSocket);

            await clientWebSocket.RequestNextItem(cancellationToken);

            var enumerator = clientWebSocket.Read(cancellationToken).GetAsyncEnumerator(cancellationToken);

            while (true)
            {
                try
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        await clientWebSocket.Close(cancellationToken);
                        yield break;
                    }
                }
                catch (OperationCanceledException)
                {
                    await clientWebSocket.Close(CancellationToken.None);
                    throw;
                }
                catch
                {
                    await clientWebSocket.Close(cancellationToken);
                    throw;
                }
                
                if (enumerator.Current is StreamingMessageEnvelope<TItem> { Message: { } } env)
                {
                    yield return env.Message;
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
}
