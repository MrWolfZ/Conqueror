namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore;

using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

internal static class DynamicStreamingControllerFactory
{
    private static readonly AssemblyBuilder DynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(
        new("ConquerorStreamingTransportHttpServerAspNetCoreDynamic"),
        AssemblyBuilderAccess.Run
    );

    private static readonly ModuleBuilder ModuleBuilder = DynamicAssembly.DefineDynamicModule(
        "ConquerorStreamingTransportHttpServerAspNetCoreDynamicModule"
    );

    private static readonly ConcurrentDictionary<string, Lazy<Type>> DynamicTypeDictionary = [];

    public static Type Create(string name, Func<Type> typeFactory) =>
        DynamicTypeDictionary.GetOrAdd(name, static (_, typeFactory) => new(typeFactory), typeFactory).Value;

    public static TypeBuilder CreateTypeBuilder(string name, HttpEndpoint endpoint)
    {
        var typeName = $"{name}`ConquerorStreamingTransportHttpServerAspNetCoreDynamicController";
        var typeBuilder = ModuleBuilder.DefineType(
            typeName,
            TypeAttributes.NotPublic | TypeAttributes.Sealed,
            typeof(ControllerBase)
        );

        SetApiControllerAttribute(typeBuilder);
        SetRouteAttribute(typeBuilder, endpoint.Path);
        SetControllerRouteValueAttribute(typeBuilder, endpoint.ControllerName);
        SetApiExplorerSettingsAttribute(typeBuilder, endpoint.ApiGroupName);

        return typeBuilder;
    }

    public static void ApplyHttpMethodAttribute(MethodBuilder methodBuilder, Type attributeType, string name)
    {
        var ctor = attributeType.GetConstructors().First(c => c.GetParameters().Length is 0);
        var nameParam = attributeType
            .GetProperties()
            .First(p => string.Equals(p.Name, nameof(HttpMethodAttribute.Name), StringComparison.Ordinal));
        var attributeBuilder = new CustomAttributeBuilder(ctor, [], [nameParam], [name]);
        methodBuilder.SetCustomAttribute(attributeBuilder);
    }

    public static void ApplyProducesResponseTypeAttribute(MethodBuilder methodBuilder, int statusCode)
    {
        var ctor = typeof(ProducesResponseTypeAttribute)
            .GetConstructors()
            .First(c => c.GetParameters().Length is 1 && c.GetParameters().Single().ParameterType == typeof(int));
        var attributeBuilder = new CustomAttributeBuilder(ctor, [statusCode], Array.Empty<FieldInfo>(), []);
        methodBuilder.SetCustomAttribute(attributeBuilder);
    }

    public static void ApplyParameterSourceAttribute(ParameterBuilder parameterBuilder, Type attributeType)
    {
        var ctor = attributeType.GetConstructors().First(c => c.GetParameters().Length is 0);
        var attributeBuilder = new CustomAttributeBuilder(ctor, []);
        parameterBuilder.SetCustomAttribute(attributeBuilder);
    }

    private static void SetApiControllerAttribute(TypeBuilder typeBuilder)
    {
        var ctor = typeof(ApiControllerAttribute).GetConstructors().First(c => c.GetParameters().Length is 0);
        var attributeBuilder = new CustomAttributeBuilder(ctor, []);
        typeBuilder.SetCustomAttribute(attributeBuilder);
    }

    private static void SetControllerRouteValueAttribute(TypeBuilder typeBuilder, string groupName)
    {
        var ctor = typeof(ConquerorStreamingControllerRouteValueAttribute)
            .GetConstructors()
            .First(c => c.GetParameters().Length is 1 && c.GetParameters().Single().ParameterType == typeof(string));
        var attributeBuilder = new CustomAttributeBuilder(ctor, [groupName]);
        typeBuilder.SetCustomAttribute(attributeBuilder);
    }

    private static void SetApiExplorerSettingsAttribute(TypeBuilder typeBuilder, string? groupName)
    {
        if (groupName is null)
        {
            return;
        }

        var ctor = typeof(ApiExplorerSettingsAttribute).GetConstructors().First(c => c.GetParameters().Length is 0);
        var nameParam = typeof(ApiExplorerSettingsAttribute)
            .GetProperties()
            .First(p =>
                string.Equals(p.Name, nameof(ApiExplorerSettingsAttribute.GroupName), StringComparison.Ordinal)
            );
        var attributeBuilder = new CustomAttributeBuilder(ctor, [], [nameParam], [groupName]);
        typeBuilder.SetCustomAttribute(attributeBuilder);
    }

    private static void SetRouteAttribute(TypeBuilder typeBuilder, string route)
    {
        var ctor = typeof(RouteAttribute)
            .GetConstructors()
            .First(c => c.GetParameters().Length is 1 && c.GetParameters().Single().ParameterType == typeof(string));
        var attributeBuilder = new CustomAttributeBuilder(ctor, [route]);
        typeBuilder.SetCustomAttribute(attributeBuilder);
    }
}
