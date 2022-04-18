using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using Conqueror.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Conqueror.CQS.Extensions.AspNetCore.Server
{
    internal sealed class DynamicCommandControllerFactory : DynamicControllerFactory
    {
        private const string ApiGroupName = "Commands";

        public Type Create(CommandHandlerMetadata metadata, HttpCommandAttribute attribute)
        {
            return Create(metadata.CommandType.Name, () => CreateType(metadata, attribute));
        }

        private Type CreateType(CommandHandlerMetadata metadata, HttpCommandAttribute attribute)
        {
            // to be used in the future
            _ = attribute;

            var name = metadata.CommandType.Name;

            // TODO: use service
            var regex = new Regex("Command$");
            var route = $"/api/commands/{regex.Replace(name, string.Empty)}";

            var hasPayload = metadata.CommandType.HasAnyProperties() || !metadata.CommandType.HasDefaultConstructor();

            var genericBaseControllerTypeWithResponse = hasPayload ? typeof(ConquerorCommandControllerBase<,>) : typeof(ConquerorCommandWithoutPayloadControllerBase<,>);
            var genericBaseControllerTypeWithoutResponse = hasPayload ? typeof(ConquerorCommandControllerBase<>) : typeof(ConquerorCommandWithoutPayloadControllerBase<>);

            var baseControllerType = metadata.ResponseType != null
                ? genericBaseControllerTypeWithResponse.MakeGenericType(metadata.CommandType, metadata.ResponseType)
                : genericBaseControllerTypeWithoutResponse.MakeGenericType(metadata.CommandType);

            var typeBuilder = CreateTypeBuilder(name, ApiGroupName, baseControllerType, route);

            EmitExecuteMethod();

            return typeBuilder.CreateType()!;

            void EmitExecuteMethod()
            {
                var baseMethod = baseControllerType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Single(m => m.Name == "ExecuteCommand");
                var parameterTypes = baseMethod.GetParameters().Select(p => p.ParameterType).ToArray();

                var methodBuilder = typeBuilder.DefineMethod(
                    $"Execute{name}",
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    baseMethod.ReturnType,
                    parameterTypes);

                ApplyHttpMethodAttribute(methodBuilder, typeof(HttpPostAttribute), $"commands.{regex.Replace(name, string.Empty)}");

                if (hasPayload)
                {
                    var paramBuilder = methodBuilder.DefineParameter(1, ParameterAttributes.None, "command");
                    ApplyParameterSourceAttribute(paramBuilder, typeof(FromBodyAttribute));
                }

                if (metadata.ResponseType == null)
                {
                    ApplyProducesResponseTypeAttribute(methodBuilder, StatusCodes.Status204NoContent);
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
