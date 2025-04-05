using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Conqueror;
using Conqueror.Transport.Http.Server.AspNetCore.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorHttpServerMessagingEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMessageEndpoints(this IEndpointRouteBuilder builder)
    {
        var messageTransportRegistry = builder.ServiceProvider.GetRequiredService<IMessageTransportRegistry>();
        foreach (var (messageType, _, typeInjector) in messageTransportRegistry.GetMessageTypesForTransportInterface<IHttpMessage>())
        {
            if (typeInjector is not IHttpMessageTypesInjector i)
            {
                throw new InvalidOperationException($"could not get the message type injector for message type '{messageType}'");
            }

            _ = i.CreateWithMessageTypes(new EndpointTypeInjectable(builder));
        }

        return builder;
    }

    public static IEndpointConventionBuilder MapMessageEndpoint<TMessage>(this IEndpointRouteBuilder builder)
        where TMessage : class, IHttpMessage
    {
        return TMessage.HttpMessageTypesInjector.CreateWithMessageTypes(new EndpointTypeInjectable(builder));
    }

    private sealed class EndpointTypeInjectable(IEndpointRouteBuilder builder) : IHttpMessageTypesInjectable<IEndpointConventionBuilder>
    {
        public IEndpointConventionBuilder WithInjectedTypes<TMessage, TResponse>()
            where TMessage : class, IHttpMessage<TMessage, TResponse>
        {
            return ConfigureRoute<TMessage, TResponse>(
                builder.MapMethods(TMessage.FullPath, [TMessage.HttpMethod], Handle<TMessage, TResponse>),
                TMessage.EmptyInstance is null);
        }

        private static async Task Handle<TMessage, TResponse>(HttpContext context)
            where TMessage : class, IHttpMessage<TMessage, TResponse>
        {
            var message = TMessage.EmptyInstance;

            // handle messages without payload
            if (message is not null)
            {
                await Handle<TMessage, TResponse>(message, context).ConfigureAwait(false);
                return;
            }

            if (TMessage.HttpMessageSerializer is { } ms)
            {
                var query = context.Request.Query.ToDictionary(p => p.Key, IReadOnlyList<string?> (p) => p.Value.AsReadOnly());
                message = await ms.Deserialize(context.RequestServices,
                                               context.Request.Body,
                                               context.Request.Path,
                                               query,
                                               context.RequestAborted)
                                  .ConfigureAwait(false);
            }
            else
            {
                var jsonTypeInfo = GetJsonTypeInfo<TMessage>(context, TMessage.HttpJsonSerializerContext);
                message = await context.Request.ReadFromJsonAsync(jsonTypeInfo).ConfigureAwait(false);
            }

            await Handle<TMessage, TResponse>(message, context).ConfigureAwait(false);
        }

        private static async Task Handle<TMessage, TResponse>(TMessage? message, HttpContext context)
            where TMessage : class, IHttpMessage<TMessage, TResponse>
        {
            if (message is null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Invalid request").ConfigureAwait(false);
                return;
            }

            var response = await context.GetMessageClient(TMessage.T).Handle(message, context.RequestAborted).ConfigureAwait(false);

            context.Response.StatusCode = TMessage.SuccessStatusCode;

            if (typeof(TResponse) == typeof(UnitMessageResponse))
            {
                return;
            }

            if (TMessage.HttpResponseSerializer is { } rs)
            {
                await rs.Serialize(context.RequestServices, context.Response.Body, response, context.RequestAborted).ConfigureAwait(false);
                return;
            }

            var jsonTypeInfo = GetJsonTypeInfo<TResponse>(context, TMessage.HttpJsonSerializerContext);
            await context.Response.WriteAsJsonAsync(response, jsonTypeInfo).ConfigureAwait(false);
        }

        private static JsonTypeInfo<T> GetJsonTypeInfo<T>(HttpContext context, JsonSerializerContext? serializerContext)
        {
            var jsonSerializerOptions = context.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;

            // this will throw when no type info is available for the type (e.g. in AOT context); we do not do anything
            // to prevent this so that the user is informed that they must add the correct json serializer source gen
            // config; in non-AOT scenarios, there will be a default json serializer set by ASP
            return (JsonTypeInfo<T>)(serializerContext?.GetTypeInfo(typeof(T)) ?? jsonSerializerOptions.GetTypeInfo(typeof(T)));
        }

        private static IEndpointConventionBuilder ConfigureRoute<TMessage, TResponse>(IEndpointConventionBuilder builder, bool hasPayload)
            where TMessage : class, IHttpMessage<TMessage, TResponse>
        {
            builder = builder.WithMetadata(typeof(TResponse) == typeof(UnitMessageResponse)
                                               ? new ProducesResponseTypeMetadata(TMessage.SuccessStatusCode)
                                               : new(TMessage.SuccessStatusCode, typeof(TResponse), [MediaTypeNames.Application.Json]))
                             .WithMetadata(new ConquerorHttpMessageEndpointMetadata
                             {
                                 Name = TMessage.Name,
                                 HttpMethod = TMessage.HttpMethod,
                                 MessageContentType = TMessage.HttpMessageSerializer?.ContentType ?? MediaTypeNames.Application.Json,
                                 ResponseContentType = TMessage.HttpResponseSerializer?.ContentType ?? MediaTypeNames.Application.Json,
                                 MessageType = typeof(TMessage),
                                 HasPayload = hasPayload,
                                 QueryParams = TMessage.HttpMethod == ConquerorTransportHttpConstants.MethodGet ? GetQueryParams() : [],
                                 ResponseType = typeof(TResponse),
                                 SuccessStatusCode = TMessage.SuccessStatusCode,
                             })
                             .WithName(TMessage.Name);

            builder.Finally(b =>
            {
                var defaultResponseTypeMetadata = b.Metadata.OfType<ProducesResponseTypeMetadata>().FirstOrDefault(m => m.Type == typeof(object));

                if (defaultResponseTypeMetadata is null)
                {
                    return;
                }

                var index = b.Metadata.IndexOf(defaultResponseTypeMetadata);
                b.Metadata.RemoveAt(index);
            });

            return TMessage.ApiGroupName is null ? builder : builder.WithGroupName(TMessage.ApiGroupName);

            static IReadOnlyCollection<(string Name, Type Type)> GetQueryParams()
            {
                return TMessage.PublicProperties.Select(p => (p.Name, p.PropertyType)).ToArray();
            }
        }
    }
}
