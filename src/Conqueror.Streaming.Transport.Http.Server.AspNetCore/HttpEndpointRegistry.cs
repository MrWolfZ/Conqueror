using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore;

internal sealed class HttpEndpointRegistry
{
    private const string DefaultControllerName = "Streams";
    private readonly IStreamingRequestHandlerRegistry handlerRegistry;
    private readonly ConquerorStreamingHttpTransportServerAspNetCoreOptions options;

    public HttpEndpointRegistry(IStreamingRequestHandlerRegistry handlerRegistry, ConquerorStreamingHttpTransportServerAspNetCoreOptions options)
    {
        this.handlerRegistry = handlerRegistry;
        this.options = options;
    }

    public IReadOnlyCollection<HttpEndpoint> GetEndpoints()
    {
        var allEndpoints = GetStreamEndpoints().ToList();

        var duplicatePaths = allEndpoints.GroupBy(e => e.Path)
                                         .Where(g => g.Count() > 1)
                                         .Select(g => new { Path = g.Key, RequestTypes = g.Select(e => e.RequestType).ToList() })
                                         .ToList();

        if (duplicatePaths.Any())
        {
            var formattedDuplicatePaths = duplicatePaths.Select(a => $"{a.Path} => {string.Join(", ", a.RequestTypes.Select(t => t.Name))}");
            throw new InvalidOperationException($"found multiple endpoints with identical path, which is not allowed:\n{string.Join("\n", formattedDuplicatePaths)}");
        }

        return allEndpoints;
    }

    private IEnumerable<HttpEndpoint> GetStreamEndpoints()
    {
        foreach (var query in GetHttpStreams())
        {
            var attribute = query.StreamingRequestType.GetCustomAttribute<HttpStreamAttribute>()!;

            var path = options.PathConvention?.GetStreamPath(query.StreamingRequestType, attribute) ?? DefaultHttpStreamPathConvention.Instance.GetStreamPath(query.StreamingRequestType, attribute);

            var endpoint = new HttpEndpoint
            {
                Path = path,
                Version = attribute.Version,
                Name = query.StreamingRequestType.Name,
                OperationId = attribute.OperationId ?? query.StreamingRequestType.FullName ?? query.StreamingRequestType.Name,
                ControllerName = attribute.ApiGroupName ?? DefaultControllerName,
                ApiGroupName = attribute.ApiGroupName,
                RequestType = query.StreamingRequestType,
                ItemType = query.ItemType,
            };

            yield return endpoint;
        }
    }

    private IEnumerable<StreamingRequestHandlerRegistration> GetHttpStreams() => handlerRegistry.GetStreamingRequestHandlerRegistrations()
                                                                                                .Where(m => m.StreamingRequestType.GetCustomAttributes(typeof(HttpStreamAttribute), true).Any());
}
