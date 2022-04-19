using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using Conqueror.Common;
using Microsoft.AspNetCore.Mvc;

namespace Conqueror.CQS.Extensions.AspNetCore.Server
{
    internal sealed class DynamicQueryControllerFactory : DynamicControllerFactory
    {
        private const string ApiGroupName = "Queries";

        public Type Create(QueryHandlerMetadata metadata, HttpQueryAttribute attribute)
        {
            return Create(metadata.QueryType.Name, () => CreateType(metadata, attribute));
        }

        private Type CreateType(QueryHandlerMetadata metadata, HttpQueryAttribute attribute)
        {
            var name = metadata.QueryType.Name;

            // TODO: use service
            var regex = new Regex("Query$");
            var route = $"/api/queries/{regex.Replace(name, string.Empty)}";

            var hasPayload = metadata.QueryType.HasAnyProperties() || !metadata.QueryType.HasDefaultConstructor();

            var genericBaseControllerType = hasPayload ? typeof(ConquerorQueryControllerBase<,>) : typeof(ConquerorQueryWithoutPayloadControllerBase<,>);
            var baseControllerType = genericBaseControllerType.MakeGenericType(metadata.QueryType, metadata.ResponseType);

            var typeBuilder = CreateTypeBuilder(name, ApiGroupName, baseControllerType, route);

            EmitExecuteMethod();

            return typeBuilder.CreateType()!;

            void EmitExecuteMethod()
            {
                var baseMethod = baseControllerType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Single(m => m.Name == "ExecuteQuery");
                var parameterTypes = baseMethod.GetParameters().Select(p => p.ParameterType).ToArray();

                var methodBuilder = typeBuilder.DefineMethod(
                    $"Execute{name}",
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    baseMethod.ReturnType,
                    parameterTypes);

                var httpMethodAttributeType = attribute.UsePost ? typeof(HttpPostAttribute) : typeof(HttpGetAttribute);
                ApplyHttpMethodAttribute(methodBuilder, httpMethodAttributeType, $"queries.{regex.Replace(name, string.Empty)}");

                if (hasPayload)
                {
                    var paramBuilder = methodBuilder.DefineParameter(1, ParameterAttributes.None, "query");

                    var dataSourceAttributeType = attribute.UsePost ? typeof(FromBodyAttribute) : typeof(FromQueryAttribute);
                    ApplyParameterSourceAttribute(paramBuilder, dataSourceAttributeType);
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
}
