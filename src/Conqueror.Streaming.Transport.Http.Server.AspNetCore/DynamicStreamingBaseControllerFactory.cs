using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Mvc;

namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore;

internal abstract class DynamicStreamingBaseControllerFactory
{
    private static readonly AssemblyBuilder DynamicAssembly =
        AssemblyBuilder.DefineDynamicAssembly(new("ConquerorStreamingExtensionsAspNetCoreServerDynamic"), AssemblyBuilderAccess.Run);

    private static readonly ModuleBuilder ModuleBuilder = DynamicAssembly.DefineDynamicModule("ConquerorStreamingExtensionsAspNetCoreServerDynamicModule");
    private static readonly ConcurrentDictionary<string, Lazy<Type>> DynamicTypeDictionary = new();

    protected static Type Create(string name, Func<Type> typeFactory)
    {
        return DynamicTypeDictionary.GetOrAdd(name, _ => new(typeFactory)).Value;
    }

    protected static TypeBuilder CreateTypeBuilder(string name, string groupName, Type baseControllerType, string route)
    {
        var typeName = $"{name}`ConquerorStreamingDynamicController";
        var typeBuilder = ModuleBuilder.DefineType(typeName, TypeAttributes.NotPublic | TypeAttributes.Sealed, baseControllerType);

        SetRouteAttribute(typeBuilder, route);
        SetControllerRouteValueAttribute(typeBuilder, groupName);

        return typeBuilder;
    }

    protected static void ApplyHttpMethodAttribute(MethodBuilder methodBuilder, Type attributeType, string name)
    {
        var ctor = attributeType.GetConstructors().First(c => c.GetParameters().Length == 0);
        var nameParam = attributeType.GetProperties().First(p => p.Name == "Name");
        var attributeBuilder = new CustomAttributeBuilder(ctor, Array.Empty<object>(), new[] { nameParam }, new object[] { name });
        methodBuilder.SetCustomAttribute(attributeBuilder);
    }

    protected static void ApplyProducesResponseTypeAttribute(MethodBuilder methodBuilder, int statusCode)
    {
        var ctor = typeof(ProducesResponseTypeAttribute).GetConstructors().First(c => c.GetParameters().Length == 1 && c.GetParameters().Single().ParameterType == typeof(int));
        var attributeBuilder = new CustomAttributeBuilder(ctor, new object[] { statusCode }, Array.Empty<FieldInfo>(), Array.Empty<object>());
        methodBuilder.SetCustomAttribute(attributeBuilder);
    }

    protected static void ApplyParameterSourceAttribute(ParameterBuilder parameterBuilder, Type attributeType)
    {
        var ctor = attributeType.GetConstructors().First(c => c.GetParameters().Length == 0);
        var attributeBuilder = new CustomAttributeBuilder(ctor, Array.Empty<object>());
        parameterBuilder.SetCustomAttribute(attributeBuilder);
    }

    private static void SetControllerRouteValueAttribute(TypeBuilder typeBuilder, string groupName)
    {
        var ctor = typeof(ConquerorStreamingControllerRouteValueAttribute).GetConstructors()
                                                                          .First(c => c.GetParameters().Length == 1 && c.GetParameters().Single().ParameterType == typeof(string));
        var attributeBuilder = new CustomAttributeBuilder(ctor, new object[] { groupName });
        typeBuilder.SetCustomAttribute(attributeBuilder);
    }

    private static void SetRouteAttribute(TypeBuilder typeBuilder, string route)
    {
        var ctor = typeof(RouteAttribute).GetConstructors().First(c => c.GetParameters().Length == 1 && c.GetParameters().Single().ParameterType == typeof(string));
        var attributeBuilder = new CustomAttributeBuilder(ctor, new object[] { route });
        typeBuilder.SetCustomAttribute(attributeBuilder);
    }
}
