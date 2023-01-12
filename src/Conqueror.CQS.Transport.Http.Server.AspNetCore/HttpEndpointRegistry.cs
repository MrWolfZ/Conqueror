using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore
{
    internal sealed class HttpEndpointRegistry
    {
        private const string DefaultCommandControllerName = "Commands";
        private const string DefaultQueryControllerName = "Queries";

        private readonly ICommandHandlerRegistry commandHandlerRegistry;
        private readonly ConquerorCqsHttpTransportServerAspNetCoreOptions options;
        private readonly IQueryHandlerRegistry queryHandlerRegistry;

        public HttpEndpointRegistry(ICommandHandlerRegistry commandHandlerRegistry, IQueryHandlerRegistry queryHandlerRegistry, ConquerorCqsHttpTransportServerAspNetCoreOptions options)
        {
            this.commandHandlerRegistry = commandHandlerRegistry;
            this.queryHandlerRegistry = queryHandlerRegistry;
            this.options = options;
        }

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
            foreach (var command in GetHttpCommands())
            {
                var attribute = command.CommandType.GetCustomAttribute<HttpCommandAttribute>()!;

                var path = options.CommandPathConvention?.GetCommandPath(command.CommandType, attribute) ?? DefaultHttpCommandPathConvention.Instance.GetCommandPath(command.CommandType, attribute);

                var endpoint = new HttpEndpoint
                {
                    EndpointType = HttpEndpointType.Command,
                    Method = HttpMethod.Post,
                    Path = path,
                    Version = attribute.Version,
                    Name = command.CommandType.Name,
                    OperationId = attribute.OperationId ?? command.CommandType.FullName ?? command.CommandType.Name,
                    ControllerName = attribute.ApiGroupName ?? DefaultCommandControllerName,
                    ApiGroupName = attribute.ApiGroupName,
                    RequestType = command.CommandType,
                    ResponseType = command.ResponseType,
                };

                yield return endpoint;
            }
        }

        private IEnumerable<HttpEndpoint> GetQueryEndpoints()
        {
            foreach (var query in GetHttpQueries())
            {
                var attribute = query.QueryType.GetCustomAttribute<HttpQueryAttribute>()!;

                var path = options.QueryPathConvention?.GetQueryPath(query.QueryType, attribute) ?? DefaultHttpQueryPathConvention.Instance.GetQueryPath(query.QueryType, attribute);

                var endpoint = new HttpEndpoint
                {
                    EndpointType = HttpEndpointType.Query,
                    Method = attribute.UsePost ? HttpMethod.Post : HttpMethod.Get,
                    Path = path,
                    Version = attribute.Version,
                    Name = query.QueryType.Name,
                    OperationId = attribute.OperationId ?? query.QueryType.FullName ?? query.QueryType.Name,
                    ControllerName = attribute.ApiGroupName ?? DefaultQueryControllerName,
                    ApiGroupName = attribute.ApiGroupName,
                    RequestType = query.QueryType,
                    ResponseType = query.ResponseType,
                };

                yield return endpoint;
            }
        }

        private IEnumerable<CommandHandlerRegistration> GetHttpCommands() => commandHandlerRegistry.GetCommandHandlerRegistrations()
                                                                                                   .Where(m => m.CommandType.GetCustomAttributes(typeof(HttpCommandAttribute), true).Any());

        private IEnumerable<QueryHandlerRegistration> GetHttpQueries() => queryHandlerRegistry.GetQueryHandlerRegistrations()
                                                                                              .Where(m => m.QueryType.GetCustomAttributes(typeof(HttpQueryAttribute), true).Any());
    }
}
