namespace Conqueror.Streaming;

using System.Reflection;

internal static class TypeExtensions
{
    public static MethodInfo? GetMethodWithParameters(this Type t, string name, Type[] parameterTypes)
    {
        var methods = t.GetMethods()
            .Concat(t.GetInterfaces().SelectMany(i => i.GetMethods()))
            .Where(m => string.Equals(m.Name, name, StringComparison.Ordinal));

        return methods.FirstOrDefault(m => m.HasParameters(parameterTypes));
    }

    public static IEnumerable<MethodInfo> AllMethods(this Type t) =>
        t.GetInterfaces().Concat([t]).SelectMany(s => s.GetMethods()).Where(mi => !mi.IsStatic);

    private static bool HasParameters(this MethodInfo method, Type[] parameterTypes)
    {
        var methodParameters = method.GetParameters().Select(parameter => parameter.ParameterType).ToArray();

        if (methodParameters.Length != parameterTypes.Length)
        {
            return false;
        }

        for (var i = 0; i < methodParameters.Length; i++)
        {
            if (!string.Equals(methodParameters[i].ToString(), parameterTypes[i].ToString(), StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
