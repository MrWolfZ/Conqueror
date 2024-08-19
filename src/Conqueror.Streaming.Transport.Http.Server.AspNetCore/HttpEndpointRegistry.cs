using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore;

internal sealed class HttpEndpointRegistry
{
    private const string DefaultControllerName = "Streams";
    private readonly ConquerorStreamingHttpTransportServerAspNetCoreOptions options;
    private readonly IStreamProducerRegistry producerRegistry;

    public HttpEndpointRegistry(IStreamProducerRegistry producerRegistry, ConquerorStreamingHttpTransportServerAspNetCoreOptions options)
    {
        this.producerRegistry = producerRegistry;
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
            var attribute = query.RequestType.GetCustomAttribute<HttpStreamAttribute>()!;

            var path = options.PathConvention?.GetStreamPath(query.RequestType, attribute) ?? DefaultHttpStreamPathConvention.Instance.GetStreamPath(query.RequestType, attribute);

            var endpoint = new HttpEndpoint
            {
                Path = path,
                Version = attribute.Version,
                Name = query.RequestType.Name,
                OperationId = attribute.OperationId ?? query.RequestType.FullName ?? query.RequestType.Name,
                ControllerName = attribute.ApiGroupName ?? DefaultControllerName,
                ApiGroupName = attribute.ApiGroupName,
                RequestType = query.RequestType,
                ItemType = query.ItemType,
            };

            yield return endpoint;
        }
    }

    private IEnumerable<StreamProducerRegistration> GetHttpStreams() => producerRegistry.GetStreamProducerRegistrations()
                                                                                        .Where(m => m.RequestType.GetCustomAttributes(typeof(HttpStreamAttribute), true).Any());
}
