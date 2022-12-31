using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using Conqueror.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore
{
    internal static class DynamicCqsEndpointControllerFactory
    {
        public static Type Create(HttpEndpoint endpoint)
        {
            var typeName = endpoint.RequestType.FullName ?? endpoint.RequestType.Name;

            return DynamicCqsControllerFactory.Create(typeName, () => CreateType(typeName, endpoint));
        }

        private static Type CreateType(string typeName, HttpEndpoint endpoint)
        {
            var hasPayload = endpoint.RequestType.HasAnyProperties() || !endpoint.RequestType.HasDefaultConstructor();
            var hasResponse = endpoint.ResponseType != null;

            var typeBuilder = DynamicCqsControllerFactory.CreateTypeBuilder(typeName, endpoint);

            EmitExecuteMethod();

            return typeBuilder.CreateType()!;

            void EmitExecuteMethod()
            {
                var executorMethod = GetExecutorMethod();

                executorMethod = hasResponse ? executorMethod.MakeGenericMethod(endpoint.RequestType, endpoint.ResponseType!) : executorMethod.MakeGenericMethod(endpoint.RequestType);

                var httpContextProperty = typeof(ControllerBase).GetProperty(nameof(ControllerBase.HttpContext), BindingFlags.Public | BindingFlags.Instance);

                var parameterTypes = executorMethod.GetParameters().Select(p => p.ParameterType).Skip(1).ToArray();

                var methodBuilder = typeBuilder.DefineMethod(
                    $"Execute{endpoint.Name}",
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    executorMethod.ReturnType,
                    parameterTypes);

                var httpMethodAttributeType = endpoint.Method == HttpMethod.Post ? typeof(HttpPostAttribute) : typeof(HttpGetAttribute);
                DynamicCqsControllerFactory.ApplyHttpMethodAttribute(methodBuilder, httpMethodAttributeType, endpoint.OperationId);

                if (hasPayload)
                {
                    var paramBuilder = methodBuilder.DefineParameter(1, ParameterAttributes.None, endpoint.EndpointType == HttpEndpointType.Command ? "command" : "query");
                    var dataSourceAttributeType = endpoint.Method == HttpMethod.Post ? typeof(FromBodyAttribute) : typeof(FromQueryAttribute);
                    DynamicCqsControllerFactory.ApplyParameterSourceAttribute(paramBuilder, dataSourceAttributeType);
                }

                if (!hasResponse)
                {
                    DynamicCqsControllerFactory.ApplyProducesResponseTypeAttribute(methodBuilder, StatusCodes.Status204NoContent);
                }

                _ = methodBuilder.DefineParameter(parameterTypes.Length, ParameterAttributes.None, "cancellationToken");

                var ilGenerator = methodBuilder.GetILGenerator();

                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Callvirt, httpContextProperty?.GetGetMethod()!);
                ilGenerator.Emit(OpCodes.Ldarg_1);

                if (hasPayload)
                {
                    ilGenerator.Emit(OpCodes.Ldarg_2);
                }

                ilGenerator.Emit(OpCodes.Call, executorMethod);
                ilGenerator.Emit(OpCodes.Ret);
            }

            MethodInfo GetExecutorMethod()
            {
                return endpoint.EndpointType switch
                {
                    HttpEndpointType.Command => GetCommandExecutorMethod(),
                    HttpEndpointType.Query => GetQueryExecutorMethod(),
                    _ => throw new ArgumentOutOfRangeException($"unknown endpoint type '{endpoint.EndpointType}'"),
                };
            }

            MethodInfo GetCommandExecutorMethod()
            {
                var executionMethods = typeof(HttpCommandExecutor).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                                                  .Where(m => m.Name == nameof(HttpCommandExecutor.ExecuteCommand));

                return hasPayload switch
                {
                    true when hasResponse => executionMethods.Single(m => m.GetGenericArguments().Length == 2 && m.GetParameters().Length == 3),
                    true when !hasResponse => executionMethods.Single(m => m.GetGenericArguments().Length == 1 && m.GetParameters().Length == 3),
                    false when hasResponse => executionMethods.Single(m => m.GetGenericArguments().Length == 2 && m.GetParameters().Length == 2),
                    _ => executionMethods.Single(m => m.GetGenericArguments().Length == 1 && m.GetParameters().Length == 2),
                };
            }

            MethodInfo GetQueryExecutorMethod()
            {
                var executionMethods = typeof(HttpQueryExecutor).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                                                .Where(m => m.Name == nameof(HttpQueryExecutor.ExecuteQuery));

                return hasPayload switch
                {
                    true => executionMethods.Single(m => m.GetParameters().Length == 3),
                    false => executionMethods.Single(m => m.GetParameters().Length == 2),
                };
            }
        }
    }
}
