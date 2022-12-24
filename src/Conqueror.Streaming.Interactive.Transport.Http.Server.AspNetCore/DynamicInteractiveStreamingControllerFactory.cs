using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using Conqueror.Common;
using Conqueror.Streaming.Interactive.Common;
using Microsoft.AspNetCore.Mvc;

namespace Conqueror.Streaming.Interactive.Transport.Http.Server.AspNetCore
{
    internal sealed class DynamicInteractiveStreamingControllerFactory : DynamicInteractiveStreamingBaseControllerFactory
    {
        private const string ApiGroupName = "InteractiveStreams";

        public Type Create(InteractiveStreamingHandlerMetadata metadata, HttpInteractiveStreamingRequestAttribute attribute)
        {
            var typeName = metadata.RequestType.FullName ?? metadata.RequestType.Name;

            return Create(typeName, () => CreateType(typeName, metadata, attribute));
        }

        private Type CreateType(string typeName, InteractiveStreamingHandlerMetadata metadata, HttpInteractiveStreamingRequestAttribute attribute)
        {
            var name = metadata.RequestType.Name;

            // TODO: use service
            var regex = new Regex("(Interactive)?(Stream(ing)?)?Request$");
            var route = $"/api/streams/interactive/{regex.Replace(name, string.Empty)}";

            var hasPayload = metadata.RequestType.HasAnyProperties() || !metadata.RequestType.HasDefaultConstructor();

            var genericBaseControllerType = hasPayload ? typeof(ConquerorInteractiveStreamingWithRequestPayloadWebsocketTransportControllerBase<,>) : typeof(ConquerorInteractiveStreamingWithoutRequestPayloadWebsocketTransportControllerBase<,>);
            var baseControllerType = genericBaseControllerType.MakeGenericType(metadata.RequestType, metadata.ItemType);

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

                ApplyHttpMethodAttribute(methodBuilder, typeof(HttpGetAttribute), $"streams.interactive.{regex.Replace(name, string.Empty)}");

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
}
