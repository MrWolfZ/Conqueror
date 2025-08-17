namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore;

internal static class TypeExtensions
{
    public static bool HasAnyProperties(this Type t) => t.GetProperties().Length is not 0;

    public static bool HasDefaultConstructor(this Type t) =>
        Array.Exists(t.GetConstructors(), c => c.GetParameters().Length is 0);
}
