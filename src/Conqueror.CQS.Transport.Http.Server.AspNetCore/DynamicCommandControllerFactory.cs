using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using Conqueror.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore
{
    internal sealed class DynamicCommandControllerFactory : DynamicCqsControllerFactory
    {
        private const string ApiGroupName = "Commands";

        public Type Create(CommandHandlerRegistration registration, HttpCommandAttribute attribute)
        {
            var typeName = registration.CommandType.FullName ?? registration.CommandType.Name;

            return Create(typeName, () => CreateType(typeName, registration, attribute));
        }

        private Type CreateType(string typeName, CommandHandlerRegistration registration, HttpCommandAttribute attribute)
        {
            // to be used in the future
            _ = attribute;

            var name = registration.CommandType.Name;

            // TODO: use service
            var regex = new Regex("Command$");
            var route = $"/api/commands/{regex.Replace(name, string.Empty)}";

            var hasPayload = registration.CommandType.HasAnyProperties() || !registration.CommandType.HasDefaultConstructor();

            var genericBaseControllerTypeWithResponse = hasPayload ? typeof(ConquerorCommandControllerBase<,>) : typeof(ConquerorCommandWithoutPayloadControllerBase<,>);
            var genericBaseControllerTypeWithoutResponse = hasPayload ? typeof(ConquerorCommandControllerBase<>) : typeof(ConquerorCommandWithoutPayloadControllerBase<>);

            var baseControllerType = registration.ResponseType != null
                ? genericBaseControllerTypeWithResponse.MakeGenericType(registration.CommandType, registration.ResponseType)
                : genericBaseControllerTypeWithoutResponse.MakeGenericType(registration.CommandType);

            var typeBuilder = CreateTypeBuilder(typeName, ApiGroupName, baseControllerType, route);

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

                if (registration.ResponseType == null)
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
