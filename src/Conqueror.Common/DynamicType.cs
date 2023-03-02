using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Conqueror.Common;

internal static class DynamicType
{
    private static readonly AssemblyBuilder DynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(new("ConquerorDynamic"), AssemblyBuilderAccess.Run);
    private static readonly ModuleBuilder ModuleBuilder = DynamicAssembly.DefineDynamicModule("ConquerorDynamicModule");
    private static readonly ConcurrentDictionary<(Type, Type), Lazy<Type>> DynamicTypeDictionary = new();

    public static Type Create(Type interfaceType, Type targetType)
    {
        return DynamicTypeDictionary.GetOrAdd((interfaceType, targetType), t => new(() => GenerateType(t.Item1, t.Item2))).Value;
    }

    private static Type GenerateType(Type interfaceType, Type targetType)
    {
        var typeName = $"{interfaceType.FullName}_{targetType.FullName}_Dynamic";
        var typeBuilder = ModuleBuilder.DefineType(typeName, TypeAttributes.NotPublic | TypeAttributes.Sealed, null, new[] { interfaceType });

        var targetFieldBuilder = typeBuilder.DefineField(
            "target",
            targetType,
            FieldAttributes.Private);

        EmitConstructor(typeBuilder, targetFieldBuilder, targetType);

        foreach (var method in interfaceType.AllMethods())
        {
            EmitMethod(method, targetType, typeBuilder, targetFieldBuilder);
        }

        return typeBuilder.CreateType()!;
    }

    private static void EmitConstructor(TypeBuilder typeBuilder, FieldInfo targetField, Type targetType)
    {
        var ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { targetType });

        var ilGenerator = ctorBuilder.GetILGenerator();

        // For a constructor, argument zero is a reference to the new
        // instance. Push it on the stack before calling the base
        // class constructor. Specify the default constructor of the
        // base class (System.Object) by passing an empty array of
        // types (Type.EmptyTypes) to GetConstructor.
        ilGenerator.Emit(OpCodes.Ldarg_0);
        ilGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);

        // Push the instance on the stack before pushing the argument
        // that is to be assigned to the private field.
        ilGenerator.Emit(OpCodes.Ldarg_0);
        ilGenerator.Emit(OpCodes.Ldarg_1);
        ilGenerator.Emit(OpCodes.Stfld, targetField);
        ilGenerator.Emit(OpCodes.Ret);
    }

    private static void EmitMethod(MethodInfo methodDefinition, Type targetType, TypeBuilder typeBuilder, FieldInfo targetField)
    {
        var parameters = methodDefinition.GetParameters().ToList();
        var parameterTypes = parameters.Select(parameter => parameter.ParameterType).ToArray();

        var method = targetType.GetMethodWithParameters(methodDefinition.Name, parameterTypes);
        if (method == null)
        {
            throw new MissingMethodException(targetType.Name, methodDefinition.Name);
        }

        var methodBuilder = typeBuilder.DefineMethod(
            methodDefinition.Name,
            MethodAttributes.Public | MethodAttributes.Virtual,
            methodDefinition.ReturnType,
            parameterTypes);

        if (method.IsGenericMethod)
        {
            _ = methodBuilder.DefineGenericParameters(
                methodDefinition.GetGenericArguments().Select(arg => arg.Name).ToArray());
        }

        var ilGenerator = methodBuilder.GetILGenerator();

        ilGenerator.Emit(OpCodes.Ldarg_0);

        ilGenerator.Emit(OpCodes.Ldfld, targetField);

        for (var i = 1; i < parameters.Count + 1; i++)
        {
            ilGenerator.Emit(OpCodes.Ldarg, i);
        }

        var callOpCode = method.DeclaringType?.IsInterface ?? false ? OpCodes.Callvirt : OpCodes.Call;

        ilGenerator.Emit(callOpCode, method);
        ilGenerator.Emit(OpCodes.Ret);
    }
}
