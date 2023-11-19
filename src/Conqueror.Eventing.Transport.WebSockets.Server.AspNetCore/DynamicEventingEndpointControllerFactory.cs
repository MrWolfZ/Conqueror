using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Conqueror.Eventing.Transport.WebSockets.Server.AspNetCore;

internal static class DynamicEventingEndpointControllerFactory
{
    public static Type Create(HttpEndpoint endpoint)
    {
        return DynamicEventingControllerFactory.Create(endpoint.ControllerName, () => CreateType(endpoint));
    }

    private static Type CreateType(HttpEndpoint endpoint)
    {
        var typeBuilder = DynamicEventingControllerFactory.CreateTypeBuilder(endpoint.ControllerName, endpoint);

        EmitObserveMethod();

        return typeBuilder.CreateType()!;

        void EmitObserveMethod()
        {
            var connectionHandlingMethod = GetConnectionHandlingMethod();

            var httpContextProperty = typeof(ControllerBase).GetProperty(nameof(ControllerBase.HttpContext), BindingFlags.Public | BindingFlags.Instance);

            var parameterTypes = connectionHandlingMethod.GetParameters().Select(p => p.ParameterType).Skip(1).ToArray();

            var methodBuilder = typeBuilder.DefineMethod(
                $"Observe{endpoint.Name}",
                MethodAttributes.Public | MethodAttributes.Virtual,
                connectionHandlingMethod.ReturnType,
                parameterTypes);

            DynamicEventingControllerFactory.ApplyHttpMethodAttribute(methodBuilder, typeof(HttpGetAttribute), endpoint.OperationId);
            
            var paramBuilder = methodBuilder.DefineParameter(1, ParameterAttributes.None, "eventTypeId");
            var dataSourceAttributeType = typeof(FromQueryAttribute);
            DynamicEventingControllerFactory.ApplyParameterSourceAttribute(paramBuilder, dataSourceAttributeType);

            DynamicEventingControllerFactory.ApplyProducesResponseTypeAttribute(methodBuilder, StatusCodes.Status200OK);

            _ = methodBuilder.DefineParameter(parameterTypes.Length, ParameterAttributes.None, "cancellationToken");

            var ilGenerator = methodBuilder.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Callvirt, httpContextProperty?.GetGetMethod()!);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Ldarg_2);

            ilGenerator.Emit(OpCodes.Call, connectionHandlingMethod);
            ilGenerator.Emit(OpCodes.Ret);
        }

        MethodInfo GetConnectionHandlingMethod()
        {
            return typeof(WebSocketsConnectionHandler)
                   .GetMethods(BindingFlags.Public | BindingFlags.Static)
                   .Single(m => m.Name == nameof(WebSocketsConnectionHandler.HandleWebSocketConnection));
        }
    }
}
