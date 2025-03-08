using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore;

internal sealed class HttpEndpointRegistry(
    ICommandTransportRegistry commandTransportRegistry,
    IQueryTransportRegistry queryTransportRegistry,
    ConquerorCqsHttpTransportServerAspNetCoreOptions options)
{
    private const string DefaultCommandControllerName = "Commands";
    private const string DefaultQueryControllerName = "Queries";

    public IReadOnlyCollection<HttpEndpoint> GetEndpoints()
    {
        var allEndpoints = GetCommandEndpoints().Concat(GetQueryEndpoints()).ToList();

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

    private IEnumerable<HttpEndpoint> GetCommandEndpoints()
    {
        foreach (var (commandType, responseType, attribute) in commandTransportRegistry.GetCommandTypesForTransport<HttpCommandAttribute>())
        {
            var path = options.CommandPathConvention?.GetCommandPath(commandType, attribute) ?? DefaultHttpCommandPathConvention.Instance.GetCommandPath(commandType, attribute);

            var endpoint = new HttpEndpoint
            {
                EndpointType = HttpEndpointType.Command,
                Method = HttpMethod.Post,
                Path = path,
                Version = attribute.Version,
                Name = commandType.Name,
                OperationId = attribute.OperationId ?? commandType.FullName ?? commandType.Name,
                ControllerName = attribute.ApiGroupName ?? DefaultCommandControllerName,
                ApiGroupName = attribute.ApiGroupName,
                RequestType = commandType,
                ResponseType = responseType,
            };

            yield return endpoint;
        }
    }

    private IEnumerable<HttpEndpoint> GetQueryEndpoints()
    {
        foreach (var (queryType, responseType, attribute) in queryTransportRegistry.GetQueryTypesForTransport<HttpQueryAttribute>())
        {
            var path = options.QueryPathConvention?.GetQueryPath(queryType, attribute) ?? DefaultHttpQueryPathConvention.Instance.GetQueryPath(queryType, attribute);

            var endpoint = new HttpEndpoint
            {
                EndpointType = HttpEndpointType.Query,
                Method = attribute.UsePost ? HttpMethod.Post : HttpMethod.Get,
                Path = path,
                Version = attribute.Version,
                Name = queryType.Name,
                OperationId = attribute.OperationId ?? queryType.FullName ?? queryType.Name,
                ControllerName = attribute.ApiGroupName ?? DefaultQueryControllerName,
                ApiGroupName = attribute.ApiGroupName,
                RequestType = queryType,
                ResponseType = responseType,
            };

            yield return endpoint;
        }
    }
}
