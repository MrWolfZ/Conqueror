using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Conqueror.CQS.Transport.Http.Common;

namespace Conqueror.CQS.Transport.Http.Client;

internal sealed class HttpQueryTransportClient : IQueryTransportClient
{
    private readonly IConquerorContextAccessor conquerorContextAccessor;

    public HttpQueryTransportClient(ResolvedHttpClientOptions options, IConquerorContextAccessor conquerorContextAccessor)
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

        SetHeaders(requestMessage.Headers);

        TracingHelper.SetTraceParentHeaderForTestClient(requestMessage.Headers, Options.HttpClient);

        var uriString = Options.QueryPathConvention?.GetQueryPath(typeof(TQuery), attribute) ?? DefaultHttpQueryPathConvention.Instance.GetQueryPath(typeof(TQuery), attribute);

        if (attribute.UsePost)
        {
            requestMessage.Content = JsonContent.Create(query, null, Options.JsonSerializerOptions);
        }
        else
        {
            uriString += BuildQueryString(query);
        }

        requestMessage.RequestUri = new(Options.BaseAddress, uriString);

        try
        {
            var response = await Options.HttpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var content = await response.BufferAndReadContent().ConfigureAwait(false);
                throw new HttpQueryFailedException($"query of type {typeof(TQuery).Name} failed with status code {response.StatusCode} and response content: {content}", response);
            }

            if (conquerorContextAccessor.ConquerorContext is { } ctx && response.Headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var values))
            {
                var parsedContextItems = ContextValueFormatter.Parse(values);

                foreach (var (key, value) in parsedContextItems)
                {
                    ctx.UpstreamContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
                }
            }

            var result = await response.Content.ReadFromJsonAsync<TResponse>(Options.JsonSerializerOptions, cancellationToken).ConfigureAwait(false);
            return result!;
        }
        catch (Exception ex) when (ex is not HttpQueryFailedException)
        {
            throw new HttpQueryFailedException($"query of type {typeof(TQuery).Name} failed", null, ex);
        }
    }

    private void SetHeaders(HttpHeaders headers)
    {
        if (Activity.Current is null && conquerorContextAccessor.ConquerorContext?.TraceId is { } traceId)
        {
            headers.Add(HttpConstants.TraceParentHeaderName, TracingHelper.CreateTraceParent(traceId: traceId));
        }

        if (ContextValueFormatter.Format(conquerorContextAccessor.ConquerorContext?.DownstreamContextData) is { } s)
        {
            headers.Add(HttpConstants.ConquerorContextHeaderName, s);
        }

        if (Options.Headers is { } headersFromOptions)
        {
            foreach (var (headerName, headerValues) in headersFromOptions)
            {
                headers.Add(headerName, headerValues);
            }
        }
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

                if (value is string s)
                {
                    queryString.Add(property.Name, s);
                }
                else if (value is IEnumerable e)
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
