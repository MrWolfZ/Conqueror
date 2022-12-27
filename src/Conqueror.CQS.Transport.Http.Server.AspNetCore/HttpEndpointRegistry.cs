using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore
{
    internal sealed class HttpEndpointRegistry
    {
        private readonly ICommandHandlerRegistry commandHandlerRegistry;
        private readonly IQueryHandlerRegistry queryHandlerRegistry;

        public HttpEndpointRegistry(ICommandHandlerRegistry commandHandlerRegistry, IQueryHandlerRegistry queryHandlerRegistry)
        {
            this.commandHandlerRegistry = commandHandlerRegistry;
            this.queryHandlerRegistry = queryHandlerRegistry;
        }

        public IReadOnlyCollection<HttpEndpoint> GetEndpoints()
        {
            return GetCommandEndpoints().Concat(GetQueryEndpoints()).ToList();
        }

        private IEnumerable<HttpEndpoint> GetCommandEndpoints()
        {
            const string apiGroupName = "Commands";

            foreach (var command in GetHttpCommands())
            {
                var attribute = command.CommandType.GetCustomAttribute<HttpCommandAttribute>()!;

                // to be used in the future
                _ = attribute;

                // TODO: use service
                var regex = new Regex("Command$");
                var route = $"/api/commands/{regex.Replace(command.CommandType.Name, string.Empty)}";

                var endpoint = new HttpEndpoint
                {
                    EndpointType = HttpEndpointType.Command,
                    Method = HttpMethod.Post,
                    Path = route,
                    Version = 1,
                    OperationId = command.CommandType.FullName ?? command.CommandType.Name,
                    Name = command.CommandType.Name,
                    ApiGroupName = apiGroupName,
                    RequestType = command.CommandType,
                    ResponseType = command.ResponseType,
                };

                yield return endpoint;
            }
        }

        private IEnumerable<HttpEndpoint> GetQueryEndpoints()
        {
            const string apiGroupName = "Queries";
            
            foreach (var query in GetHttpQueries())
            {
                var attribute = query.QueryType.GetCustomAttribute<HttpQueryAttribute>()!;

                // TODO: use service
                var regex = new Regex("Query$");
                var route = $"/api/queries/{regex.Replace(query.QueryType.Name, string.Empty)}";

                var endpoint = new HttpEndpoint
                {
                    EndpointType = HttpEndpointType.Query,
                    Method = attribute.UsePost ? HttpMethod.Post : HttpMethod.Get,
                    Path = route,
                    Version = 1,
                    OperationId = query.QueryType.FullName ?? query.QueryType.Name,
                    ApiGroupName = apiGroupName,
                    Name = query.QueryType.Name,
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
