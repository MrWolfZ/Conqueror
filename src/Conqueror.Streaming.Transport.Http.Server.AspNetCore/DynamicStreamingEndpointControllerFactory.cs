using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore;

internal static class DynamicStreamingEndpointControllerFactory
{
    public static Type Create(HttpEndpoint endpoint)
    {
        var typeName = endpoint.RequestType.FullName ?? endpoint.RequestType.Name;

        return DynamicStreamingControllerFactory.Create(typeName, () => CreateType(typeName, endpoint));
    }

    private static Type CreateType(string typeName, HttpEndpoint endpoint)
    {
        var typeBuilder = DynamicStreamingControllerFactory.CreateTypeBuilder(typeName, endpoint);

        EmitExecuteMethod();

        return typeBuilder.CreateType();

        void EmitExecuteMethod()
        {
            var executorMethod = GetExecutorMethod();

            executorMethod = executorMethod.MakeGenericMethod(endpoint.RequestType, endpoint.ItemType);

            var httpContextProperty = typeof(ControllerBase).GetProperty(nameof(ControllerBase.HttpContext), BindingFlags.Public | BindingFlags.Instance);

            var parameterTypes = executorMethod.GetParameters().Select(p => p.ParameterType).Skip(1).ToArray();

            var methodBuilder = typeBuilder.DefineMethod($"Execute{endpoint.Name}",
                                                         MethodAttributes.Public | MethodAttributes.Virtual,
                                                         executorMethod.ReturnType,
                                                         parameterTypes);

            var httpMethodAttributeType = typeof(HttpGetAttribute);
            DynamicStreamingControllerFactory.ApplyHttpMethodAttribute(methodBuilder, httpMethodAttributeType, endpoint.OperationId);

            DynamicStreamingControllerFactory.ApplyProducesResponseTypeAttribute(methodBuilder, StatusCodes.Status200OK);

            _ = methodBuilder.DefineParameter(parameterTypes.Length, ParameterAttributes.None, "cancellationToken");

            var ilGenerator = methodBuilder.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Callvirt, httpContextProperty?.GetGetMethod()!);
            ilGenerator.Emit(OpCodes.Ldarg_1);

            ilGenerator.Emit(OpCodes.Call, executorMethod);
            ilGenerator.Emit(OpCodes.Ret);
        }

        MethodInfo GetExecutorMethod()
        {
            return typeof(HttpStreamExecutor).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                             .Single(m => m.Name == nameof(HttpStreamExecutor.ExecuteStreamingRequest));
        }
    }
}
