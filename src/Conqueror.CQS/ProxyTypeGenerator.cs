using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace Conqueror.CQS;

internal static class ProxyTypeGenerator
{
    private static readonly AssemblyBuilder DynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(new("Conqueror.RuntimeGeneratedProxies"), AssemblyBuilderAccess.Run);
    private static readonly ModuleBuilder ModuleBuilder = DynamicAssembly.DefineDynamicModule("ConquerorRuntimeGeneratedProxiesModule");
    private static readonly ConcurrentDictionary<(Type, Type), Lazy<Type>> GeneratedTypes = new();

    public static Type Create(Type interfaceType, Type targetType, Type baseType)
    {
        return GeneratedTypes.GetOrAdd((interfaceType, targetType), t => new(() => GenerateType(t.Item1, t.Item2, baseType))).Value;
    }

    private static Type GenerateType(Type interfaceType, Type targetType, Type baseType)
    {
        var typeName = $"{interfaceType.FullName}_{targetType.FullName}_Dynamic";
        var typeBuilder = ModuleBuilder.DefineType(typeName, TypeAttributes.NotPublic | TypeAttributes.Sealed, baseType, [interfaceType]);

        EmitConstructor(typeBuilder, targetType, baseType);

        return typeBuilder.CreateType();
    }

    private static void EmitConstructor(TypeBuilder typeBuilder, Type targetType, Type baseType)
    {
        // find the protected constructor that takes the target instance
        var baseCtor = baseType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, [targetType], null);

        var ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, [targetType]);

        var ilGenerator = ctorBuilder.GetILGenerator();

        // For a constructor, argument zero is a reference to the new
        // instance. Push it on the stack before calling the base
        // class constructor
        ilGenerator.Emit(OpCodes.Ldarg_0);

        // call the base constructor with the constructor arg
        ilGenerator.Emit(OpCodes.Ldarg_1);
        ilGenerator.Emit(OpCodes.Call, baseCtor!);

        ilGenerator.Emit(OpCodes.Nop);
        ilGenerator.Emit(OpCodes.Ret);
    }
}
