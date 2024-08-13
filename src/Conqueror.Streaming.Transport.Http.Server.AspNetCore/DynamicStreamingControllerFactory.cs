using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;

namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore;

internal sealed class DynamicStreamingControllerFactory : DynamicStreamingBaseControllerFactory
{
    private const string ApiGroupName = "Streams";

    public Type Create(StreamingRequestHandlerRegistration registration, HttpStreamingRequestAttribute attribute)
    {
        var typeName = registration.StreamingRequestType.FullName ?? registration.StreamingRequestType.Name;

        return Create(typeName, () => CreateType(typeName, registration, attribute));
    }

    private Type CreateType(string typeName, StreamingRequestHandlerRegistration registration, HttpStreamingRequestAttribute attribute)
    {
        var name = registration.StreamingRequestType.Name;

        // TODO: use service
        var regex = new Regex("(Stream(ing)?)?Request$");
        var route = $"/api/streams/{regex.Replace(name, string.Empty)}";

        var hasPayload = registration.StreamingRequestType.HasAnyProperties() || !registration.StreamingRequestType.HasDefaultConstructor();

        var genericBaseControllerType = hasPayload ? typeof(ConquerorStreamingWithRequestPayloadWebsocketTransportControllerBase<,>) : typeof(ConquerorStreamingWithoutRequestPayloadWebsocketTransportControllerBase<,>);
        var baseControllerType = genericBaseControllerType.MakeGenericType(registration.StreamingRequestType, registration.ItemType);

        var typeBuilder = CreateTypeBuilder(typeName, ApiGroupName, baseControllerType, route);

        EmitExecuteMethod();

        return typeBuilder.CreateType()!;

        void EmitExecuteMethod()
        {
            var baseMethod = baseControllerType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Single(m => m.Name == "ExecuteRequest");
            var parameterTypes = baseMethod.GetParameters().Select(p => p.ParameterType).ToArray();

            var methodBuilder = typeBuilder.DefineMethod(
                $"Execute{name}",
                MethodAttributes.Public | MethodAttributes.Virtual,
                baseMethod.ReturnType,
                parameterTypes);

            ApplyHttpMethodAttribute(methodBuilder, typeof(HttpGetAttribute), $"streams.{regex.Replace(name, string.Empty)}");

            if (hasPayload)
            {
                var paramBuilder = methodBuilder.DefineParameter(1, ParameterAttributes.None, "request");

                ApplyParameterSourceAttribute(paramBuilder, typeof(FromQueryAttribute));
            }

            _ = methodBuilder.DefineParameter(parameterTypes.Length, ParameterAttributes.None, "cancellationToken");

            var ilGenerator = methodBuilder.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);

            if (hasPayload)
            {
                ilGenerator.Emit(OpCodes.Ldarg_2);
            }

            ilGenerator.Emit(OpCodes.Call, baseMethod);
            ilGenerator.Emit(OpCodes.Ret);
        }
    }
}
