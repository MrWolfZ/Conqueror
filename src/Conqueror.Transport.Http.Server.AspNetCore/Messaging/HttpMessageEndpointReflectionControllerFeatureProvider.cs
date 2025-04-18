using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Conqueror.Transport.Http.Server.AspNetCore.Messaging;

internal sealed class HttpMessageEndpointReflectionControllerFeatureProvider(
    IMessageTransportRegistry messageTransportRegistry,
    Predicate<Type>? messageTypeFilter)
    : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        foreach (var (messageType, _, typeInjector) in messageTransportRegistry.GetMessageTypesForTransportInterface<IHttpMessage>())
        {
            if (messageTypeFilter is not null && !messageTypeFilter(messageType))
            {
                continue;
            }

            if (typeInjector is not IHttpMessageTypesInjector i)
            {
                throw new InvalidOperationException($"could not get the HTTP message type injector for message type '{messageType}'");
            }

            var typeInfo = i.CreateWithMessageTypes(new ControllerTypeInjectable());

            if (!feature.Controllers.Contains(typeInfo))
            {
                feature.Controllers.Add(typeInfo);
            }
        }
    }

    private sealed class ControllerTypeInjectable : IHttpMessageTypesInjectable<TypeInfo>
    {
        public TypeInfo WithInjectedTypes<TMessage, TResponse>()
            where TMessage : class, IHttpMessage<TMessage, TResponse>
        {
            return (TMessage.EmptyInstance, typeof(TResponse) == typeof(UnitMessageResponse), TMessage.HttpMethod) switch
            {
                (not null, _, _) => typeof(MessageApiControllerWithoutPayload<TMessage, TResponse>).GetTypeInfo(),
                (_, true, _) => typeof(MessageApiController<TMessage, TResponse>).GetTypeInfo(),
                (_, false, ConquerorTransportHttpConstants.MethodGet) => typeof(MessageApiControllerForGet<TMessage, TResponse>).GetTypeInfo(),
                (_, false, _) => typeof(MessageApiController<TMessage, TResponse>).GetTypeInfo(),
            };
        }
    }
}
