using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore
{
    internal static class DynamicCqsControllerFactory
    {
        private static readonly AssemblyBuilder DynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(new("ConquerorCqsTransportHttpServerAspNetCoreDynamic"), AssemblyBuilderAccess.Run);
        private static readonly ModuleBuilder ModuleBuilder = DynamicAssembly.DefineDynamicModule("ConquerorCqsTransportHttpServerAspNetCoreDynamicModule");
        private static readonly ConcurrentDictionary<string, Lazy<Type>> DynamicTypeDictionary = new();

        public static Type Create(string name, Func<Type> typeFactory)
        {
            return DynamicTypeDictionary.GetOrAdd(name, _ => new(typeFactory)).Value;
        }

        public static TypeBuilder CreateTypeBuilder(string name, HttpEndpoint endpoint)
        {
            var typeName = $"{name}`ConquerorCqsDynamicController";
            var typeBuilder = ModuleBuilder.DefineType(typeName, TypeAttributes.NotPublic | TypeAttributes.Sealed, typeof(ControllerBase));

            SetApiControllerAttribute(typeBuilder);
            SetRouteAttribute(typeBuilder, endpoint.Path);
            SetControllerRouteValueAttribute(typeBuilder, endpoint.ControllerName);
            SetApiExplorerSettingsAttribute(typeBuilder, endpoint.ApiGroupName);

            return typeBuilder;
        }

        public static void ApplyHttpMethodAttribute(MethodBuilder methodBuilder, Type attributeType, string name)
        {
            var ctor = attributeType.GetConstructors().First(c => c.GetParameters().Length == 0);
            var nameParam = attributeType.GetProperties().First(p => p.Name == nameof(HttpMethodAttribute.Name));
            var attributeBuilder = new CustomAttributeBuilder(ctor, Array.Empty<object>(), new[] { nameParam }, new object[] { name });
            methodBuilder.SetCustomAttribute(attributeBuilder);
        }

        public static void ApplyProducesResponseTypeAttribute(MethodBuilder methodBuilder, int statusCode)
        {
            var ctor = typeof(ProducesResponseTypeAttribute).GetConstructors().First(c => c.GetParameters().Length == 1 && c.GetParameters().Single().ParameterType == typeof(int));
            var attributeBuilder = new CustomAttributeBuilder(ctor, new object[] { statusCode }, Array.Empty<FieldInfo>(), Array.Empty<object>());
            methodBuilder.SetCustomAttribute(attributeBuilder);
        }

        public static void ApplyParameterSourceAttribute(ParameterBuilder parameterBuilder, Type attributeType)
        {
            var ctor = attributeType.GetConstructors().First(c => c.GetParameters().Length == 0);
            var attributeBuilder = new CustomAttributeBuilder(ctor, Array.Empty<object>());
            parameterBuilder.SetCustomAttribute(attributeBuilder);
        }

        private static void SetApiControllerAttribute(TypeBuilder typeBuilder)
        {
            var ctor = typeof(ApiControllerAttribute).GetConstructors().First(c => c.GetParameters().Length == 0);
            var attributeBuilder = new CustomAttributeBuilder(ctor, Array.Empty<object>());
            typeBuilder.SetCustomAttribute(attributeBuilder);
        }

        private static void SetControllerRouteValueAttribute(TypeBuilder typeBuilder, string groupName)
        {
            var ctor = typeof(ConquerorCqsControllerRouteValueAttribute).GetConstructors().First(c => c.GetParameters().Length == 1 && c.GetParameters().Single().ParameterType == typeof(string));
            var attributeBuilder = new CustomAttributeBuilder(ctor, new object[] { groupName });
            typeBuilder.SetCustomAttribute(attributeBuilder);
        }

        private static void SetApiExplorerSettingsAttribute(TypeBuilder typeBuilder, string? groupName)
        {
            if (groupName is null)
            {
                return;
            }

            var ctor = typeof(ApiExplorerSettingsAttribute).GetConstructors().First(c => c.GetParameters().Length == 0);
            var nameParam = typeof(ApiExplorerSettingsAttribute).GetProperties().First(p => p.Name == nameof(ApiExplorerSettingsAttribute.GroupName));
            var attributeBuilder = new CustomAttributeBuilder(ctor, Array.Empty<object>(), new[] { nameParam }, new object[] { groupName });
            typeBuilder.SetCustomAttribute(attributeBuilder);
        }

        private static void SetRouteAttribute(TypeBuilder typeBuilder, string route)
        {
            var ctor = typeof(RouteAttribute).GetConstructors().First(c => c.GetParameters().Length == 1 && c.GetParameters().Single().ParameterType == typeof(string));
            var attributeBuilder = new CustomAttributeBuilder(ctor, new object[] { route });
            typeBuilder.SetCustomAttribute(attributeBuilder);
        }
    }
}
