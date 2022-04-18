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

namespace Conqueror.CQS.Extensions.AspNetCore.Client
{
    internal sealed class HttpQueryHandler<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
        where TQuery : class
    {
        private readonly HttpQueryAttribute attribute;
        private readonly HttpClient httpClient;
        private readonly JsonSerializerOptions? serializerOptions;

        public HttpQueryHandler(ResolvedHttpClientOptions options)
        {
            httpClient = options.HttpClient;
            serializerOptions = options.JsonSerializerOptions;
            attribute = typeof(TQuery).GetCustomAttribute<HttpQueryAttribute>()!;
        }

        public async Task<TResponse> ExecuteQuery(TQuery query, CancellationToken cancellationToken)
        {
            // TODO: use service
            var regex = new Regex("Query$");
            var routeQueryName = regex.Replace(typeof(TQuery).Name, string.Empty);

            var uriString = $"/api/queries/{routeQueryName}";

            if (!attribute.UsePost)
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

            var uri = new Uri(uriString, UriKind.Relative);

            var response = attribute.UsePost
                ? await httpClient.PostAsJsonAsync(uri, query, serializerOptions, cancellationToken)
                : await httpClient.GetAsync(uri, cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var content = await response.BufferAndReadContent();
                throw new HttpQueryFailedException($"query of type {typeof(TQuery).Name} failed: {content}", response);
            }

            var result = await response.Content.ReadFromJsonAsync<TResponse>(serializerOptions, cancellationToken);
            return result!;
        }
    }
}
