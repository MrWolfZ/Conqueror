using System;
using System.Collections;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Conqueror.CQS.Extensions.AspNetCore.Common;

namespace Conqueror.CQS.Extensions.AspNetCore.Client
{
    internal sealed class HttpQueryTransportClient : IQueryTransportClient
    {
        private readonly IConquerorContextAccessor? conquerorContextAccessor;
        private readonly HttpClient httpClient;
        private readonly JsonSerializerOptions? serializerOptions;

        public HttpQueryTransportClient(ResolvedHttpClientOptions options, IConquerorContextAccessor? conquerorContextAccessor)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            httpClient = options.HttpClient;
            serializerOptions = options.JsonSerializerOptions;
        }

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

            // TODO: use service
            var regex = new Regex("Query$");
            var routeQueryName = regex.Replace(typeof(TQuery).Name, string.Empty);

            var uriString = $"/api/queries/{routeQueryName}";

            if (attribute.UsePost)
            {
                requestMessage.Content = JsonContent.Create(query, null, serializerOptions);
            }
            else
            {
                var queryString = HttpUtility.ParseQueryString(string.Empty);

                foreach (var property in typeof(TQuery).GetProperties())
                {
                    var value = property.GetValue(query);

                    if (value is IEnumerable e)
                    {
                        foreach (var v in e)
                        {
                            queryString.Add(property.Name, v?.ToString());
                        }
                    }
                    else if (value is not null)
                    {
                        queryString[property.Name] = value.ToString();
                    }
                }

                if (queryString.HasKeys())
                {
                    uriString += $"?{queryString}";
                }
            }

            requestMessage.RequestUri = new(uriString, UriKind.Relative);

            var response = await httpClient.SendAsync(requestMessage, cancellationToken);

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

            var result = await response.Content.ReadFromJsonAsync<TResponse>(serializerOptions, cancellationToken);
            return result!;
        }
    }
}
