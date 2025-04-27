using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
using Microsoft.Net.Http.Headers;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorHttpServerMessagingEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMessageEndpoints(this IEndpointRouteBuilder builder)
    {
        var messageTransportRegistry = builder.ServiceProvider.GetRequiredService<IMessageHandlerRegistry>();
        foreach (var invoker in messageTransportRegistry.GetReceiverHandlerInvokers<IHttpMessageHandlerTypesInjector>())
        {
            _ = invoker.TypesInjector.Create(new EndpointTypeInjectable(builder, invoker));
        }

        return builder;
    }

    public static IEndpointConventionBuilder? MapMessageEndpoint<TMessage, TResponse, TIHandler>(this IEndpointRouteBuilder builder)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
        => builder.MapMessageEndpoint(new MessageTypes<TMessage, TResponse, TIHandler>());

    public static IEndpointConventionBuilder? MapMessageEndpoint<TMessage, TResponse, TIHandler>(this IEndpointRouteBuilder builder,
                                                                                                 MessageTypes<TMessage, TResponse, TIHandler> messageTypes)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
    {
        var handlerRegistry = builder.ServiceProvider.GetRequiredService<IMessageHandlerRegistry>();
        var invoker = handlerRegistry.GetReceiverHandlerInvoker<TMessage, TResponse, IHttpMessageHandlerTypesInjector>();

        if (invoker is null)
        {
            throw new InvalidOperationException($"either no or only a delegate handler is registered for HTTP message type '{typeof(TMessage)}'");
        }

        return invoker.TypesInjector.Create(new EndpointTypeInjectable(builder, invoker));
    }

    private sealed class EndpointTypeInjectable(IEndpointRouteBuilder builder, IMessageReceiverHandlerInvoker invoker) : IHttpMessageTypesInjectable<IEndpointConventionBuilder?>
    {
        IEndpointConventionBuilder? IHttpMessageTypesInjectable<IEndpointConventionBuilder?>.WithInjectedTypes<TMessage, TResponse, TIHandler, THandler>()
        {
            var receiver = new HttpMessageReceiver<TMessage, TResponse>(builder.ServiceProvider);
            THandler.ConfigureHttpReceiver(receiver);

            if (!receiver.IsEnabled)
            {
                return null;
            }

            var duplicates = builder.DataSources
                                    .SelectMany(ds => ds.Endpoints)
                                    .SelectMany(e => e.Metadata)
                                    .OfType<ConquerorHttpMessageEndpointMetadata>()
                                    .Where(m => m.FullPath == TMessage.FullPath && m.HttpMethod == TMessage.HttpMethod)
                                    .ToList();

            if (duplicates.Count > 0)
            {
                var duplicateMessageTypes = duplicates.Select(d => d.MessageType).Concat([typeof(TMessage)]);
                var msg = $"path: {TMessage.FullPath}{Environment.NewLine}messageTypes:{Environment.NewLine}{string.Join(Environment.NewLine, duplicateMessageTypes)}";

                throw new InvalidOperationException($"found multiple Conqueror message types with identical path!{Environment.NewLine}{msg}");
            }

            return ConfigureRoute<TMessage, TResponse>(
                builder.MapMethods(TMessage.FullPath, [TMessage.HttpMethod], Handle<TMessage, TResponse, TIHandler>),
                TMessage.EmptyInstance is null,
                receiver.IsOmittedFromApiDescription);
        }

        private async Task Handle<TMessage, TResponse, TIHandler>(HttpContext context)
            where TMessage : class, IHttpMessage<TMessage, TResponse>
            where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
        {
            var message = TMessage.EmptyInstance;

            // handle messages without payload
            if (message is not null)
            {
                await Handle<TMessage, TResponse, TIHandler>(message, context).ConfigureAwait(false);
                return;
            }

            if (TMessage.HttpMessageSerializer is { } ms)
            {
                var query = context.Request.Query.ToDictionary(p => p.Key, p => (IReadOnlyList<string?>)p.Value);
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

            await Handle<TMessage, TResponse, TIHandler>(message, context).ConfigureAwait(false);
        }

        private async Task Handle<TMessage, TResponse, TIHandler>(TMessage? message, HttpContext httpContext)
            where TMessage : class, IHttpMessage<TMessage, TResponse>
            where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
        {
            if (message is null)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsync("Invalid request").ConfigureAwait(false);
                return;
            }

            using var conquerorContext = httpContext.RequestServices.GetRequiredService<IConquerorContextAccessor>().CloneOrCreate();

            try
            {
                conquerorContext.DecodeContextData(ReadContextDataFromRequest(httpContext));
            }
            catch (FormattedConquerorContextDataInvalidException ex)
            {
                throw new MessageFailedDueToInvalidFormattedConquerorContextDataException($"badly formatted context data while processing HTTP message of type '{typeof(TMessage)}'", ex)
                {
                    MessagePayload = message,
                    TransportType = new(ConquerorTransportHttpConstants.TransportName, MessageTransportRole.Receiver),
                };
            }

            if (GetTraceId(httpContext) is { } traceId)
            {
                conquerorContext.SetTraceId(traceId);
            }

            using var principal = conquerorContext.SetCurrentPrincipalInternal(httpContext.User);

            var response = await invoker.Invoke<TMessage, TResponse>(message,
                                                                     httpContext.RequestServices,
                                                                     ConquerorTransportHttpConstants.TransportName,
                                                                     httpContext.RequestAborted)
                                        .ConfigureAwait(false);

            httpContext.Response.StatusCode = TMessage.SuccessStatusCode;

            if (conquerorContext.EncodeUpstreamContextData() is { } data)
            {
                httpContext.Response.Headers[ConquerorTransportHttpConstants.ConquerorContextHeaderName] = data;
            }

            if (typeof(TResponse) == typeof(UnitMessageResponse))
            {
                return;
            }

            if (TMessage.HttpResponseSerializer is { } rs)
            {
                await rs.Serialize(httpContext.RequestServices, httpContext.Response.Body, response, httpContext.RequestAborted).ConfigureAwait(false);
                return;
            }

            var jsonTypeInfo = GetJsonTypeInfo<TResponse>(httpContext, TMessage.HttpJsonSerializerContext);
            await httpContext.Response.WriteAsJsonAsync(response, jsonTypeInfo).ConfigureAwait(false);

            static IEnumerable<string> ReadContextDataFromRequest(HttpContext httpContext)
                => httpContext.Request.Headers.TryGetValue(ConquerorTransportHttpConstants.ConquerorContextHeaderName, out var values) ? values : [];

            static string? GetTraceId(HttpContext httpContext)
            {
                string? traceParent = null;

                if (httpContext.Request.Headers.TryGetValue(HeaderNames.TraceParent, out var traceParentValues))
                {
                    traceParent = traceParentValues.FirstOrDefault();
                }

                if (Activity.Current is null && traceParent is not null)
                {
                    using var a = new Activity(string.Empty);
                    return a.SetParentId(traceParent).TraceId.ToString();
                }

                return null;
            }
        }

        private static JsonTypeInfo<T> GetJsonTypeInfo<T>(HttpContext context, JsonSerializerContext? serializerContext)
        {
            var jsonSerializerOptions = context.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;

            // this will throw when no type info is available for the type (e.g. in AOT context); we do not do anything
            // to prevent this so that the user is informed that they must add the correct json serializer source gen
            // config; in non-AOT scenarios, there will be a default json serializer set by ASP
            return (JsonTypeInfo<T>)(serializerContext?.GetTypeInfo(typeof(T)) ?? jsonSerializerOptions.GetTypeInfo(typeof(T)));
        }

        private static IEndpointConventionBuilder ConfigureRoute<TMessage, TResponse>(IEndpointConventionBuilder builder,
                                                                                      bool hasPayload,
                                                                                      bool isOmittedFromApiDescription)
            where TMessage : class, IHttpMessage<TMessage, TResponse>
        {
            builder = builder.WithMetadata(typeof(TResponse) == typeof(UnitMessageResponse)
                                               ? new ProducesResponseTypeMetadata(TMessage.SuccessStatusCode)
                                               : new(TMessage.SuccessStatusCode, typeof(TResponse), [MediaTypeNames.Application.Json]))
                             .WithMetadata(new ConquerorHttpMessageEndpointMetadata
                             {
                                 Name = TMessage.Name,
                                 FullPath = TMessage.FullPath,
                                 ApiGroupName = TMessage.ApiGroupName,
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

            if (isOmittedFromApiDescription)
            {
                builder = builder.WithMetadata(new ExcludeFromDescriptionAttribute());
            }

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
                return TMessage.PublicProperties.Select(p => (Uncapitalize(p.Name), p.PropertyType)).ToArray();
            }

            static string Uncapitalize(string str)
                => char.ToLower(str[0], CultureInfo.InvariantCulture) + str[1..];
        }
    }
}
