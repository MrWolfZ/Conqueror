using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Conqueror.CQS.Transport.Http.Common;

namespace Conqueror.CQS.Transport.Http.Client
{
    internal sealed class HttpQueryTransportClient : IQueryTransportClient
    {
        private static readonly DefaultHttpQueryPathConvention DefaultQueryPathConvention = new();

        private readonly IConquerorContextAccessor? conquerorContextAccessor;

        public HttpQueryTransportClient(ResolvedHttpClientOptions options, IConquerorContextAccessor? conquerorContextAccessor)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            Options = options;
        }

        public ResolvedHttpClientOptions Options { get; }

        public async Task<TResponse> ExecuteQuery<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken)
            where TQuery : class
        {
            var attribute = typeof(TQuery).GetCustomAttribute<HttpQueryAttribute>()!;

            using var requestMessage = new HttpRequestMessage
            {
                Method = attribute.UsePost ? HttpMethod.Post : HttpMethod.Get,
            };

            if (conquerorContextAccessor?.ConquerorContext?.Items is { Count: > 0 } contextItems)
            {
                requestMessage.Headers.Add(HttpConstants.ConquerorContextHeaderName, ContextValueFormatter.Format(contextItems));
            }

            var uriString = Options.QueryPathConvention?.GetQueryPath(typeof(TQuery), attribute) ?? DefaultQueryPathConvention.GetQueryPath(typeof(TQuery), attribute);

            if (attribute.UsePost)
            {
                requestMessage.Content = JsonContent.Create(query, null, Options.JsonSerializerOptions);
            }
            else
            {
                uriString += BuildQueryString(query);
            }

            requestMessage.RequestUri = new(uriString, UriKind.Relative);

            var response = await Options.HttpClient.SendAsync(requestMessage, cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var content = await response.BufferAndReadContent();
                throw new HttpQueryFailedException($"query of type {typeof(TQuery).Name} failed: {content}", response);
            }

            if (conquerorContextAccessor?.ConquerorContext is { } ctx && response.Headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var values))
            {
                var parsedContextItems = ContextValueFormatter.Parse(values);
                ctx.AddOrReplaceItems(parsedContextItems);
            }

            var result = await response.Content.ReadFromJsonAsync<TResponse>(Options.JsonSerializerOptions, cancellationToken);
            return result!;
        }

        private static string BuildQueryString<TQuery>(TQuery query)
            where TQuery : class
        {
            var queryString = BuildQuery(query);

            return queryString.HasKeys() ? $"?{queryString}" : string.Empty;

            static bool IsPrimitive(object? o) => o?.GetType().IsPrimitive ?? true;

            static NameValueCollection BuildQuery(object o)
            {
                var queryString = HttpUtility.ParseQueryString(string.Empty);

                foreach (var property in o.GetType().GetProperties())
                {
                    var value = property.GetValue(o);

                    if (value is IEnumerable e)
                    {
                        foreach (var v in e)
                        {
                            queryString.Add(property.Name, v?.ToString());
                        }
                    }
                    else if (value is not null && !IsPrimitive(value))
                    {
                        var subQuery = BuildQuery(value);

                        foreach (var subProp in subQuery.AllKeys)
                        {
                            foreach (var subVal in subQuery.GetValues(subProp) ?? Enumerable.Empty<object>())
                            {
                                queryString.Add($"{property.Name}.{subProp}", subVal.ToString());
                            }
                        }
                    }
                    else if (value is not null)
                    {
                        queryString[property.Name] = value.ToString();
                    }
                }

                return queryString;
            }
        }
    }
}
