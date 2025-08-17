namespace Conqueror.Streaming;

using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

internal static class ProxyTypeGenerator
{
    private static readonly AssemblyBuilder DynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(
        new("Conqueror.RuntimeGeneratedProxies"),
        AssemblyBuilderAccess.Run
    );

    private static readonly ModuleBuilder ModuleBuilder = DynamicAssembly.DefineDynamicModule(
        "ConquerorRuntimeGeneratedProxiesModule"
    );

    private static readonly ConcurrentDictionary<(Type, Type), Lazy<Type>> GeneratedTypes = [];

    public static Type Create(Type interfaceType, Type targetType, Type baseType)
    {
        return GeneratedTypes
            .GetOrAdd(
                (interfaceType, targetType),
                static (t, bt) => new(() => GenerateType(t.Item1, t.Item2, bt)),
                baseType
            )
            .Value;
    }

    private static Type GenerateType(Type interfaceType, Type targetType, Type baseType)
    {
        var typeName = $"{interfaceType.FullName}_{targetType.FullName}_Dynamic";
        var typeBuilder = ModuleBuilder.DefineType(
            typeName,
            TypeAttributes.NotPublic | TypeAttributes.Sealed,
            baseType,
            [interfaceType]
        );

        EmitConstructor(typeBuilder, targetType, baseType);

        return typeBuilder.CreateType();
    }

    private static void EmitConstructor(TypeBuilder typeBuilder, Type targetType, Type baseType)
    {
        // find the protected constructor that takes the target instance
        var baseCtor = baseType.GetConstructor(
#pragma warning disable S3011
            BindingFlags.Instance | BindingFlags.NonPublic,
#pragma warning restore S3011
            binder: null,
            [targetType],
            modifiers: null
        );

        var ctorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            [targetType]
        );

        var generator = ctorBuilder.GetILGenerator();

        // For a constructor, argument zero is a reference to the new
        // instance. Push it on the stack before calling the base
        // class constructor
        generator.Emit(OpCodes.Ldarg_0);

        // call the base constructor with the constructor arg
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Call, baseCtor!);

        generator.Emit(OpCodes.Nop);
        generator.Emit(OpCodes.Ret);
    }
}
