using System;
using System.Collections.Generic;
using System.Linq;
using Conqueror.Common;

namespace Conqueror.CQS.CommandHandling;

internal static class CommandHandlerTypeExtensions
{
    public static IReadOnlyCollection<(Type CommandType, Type? ResponseType)> GetCommandAndResponseTypes(this Type type)
    {
        return GetCommandHandlerInterfaceTypes(type).Select(t =>
        {
            var queryType = t.GetGenericArguments().First();
            var responseType = t.GetGenericArguments().Skip(1).FirstOrDefault();
            return (queryType, responseType);
        }).ToList();
    }

    public static IReadOnlyCollection<Type> GetCommandHandlerInterfaceTypes(this Type type)
    {
        return type.GetInterfaces().Concat(new[] { type }).Where(i => i.IsCommandHandlerInterfaceType()).ToList();
    }

    public static void ValidateNoInvalidCommandHandlerInterface(this Type type)
    {
        if (!type.IsCommandHandlerInterfaceType())
        {
            var interfaces = type.GetInterfaces();
            if (interfaces.Length == 1 && interfaces[0] == typeof(ICommandHandler))
            {
                throw new ArgumentException($"type {type.Name} implements non-generic command handler interface {nameof(ICommandHandler)}");
            }
        }

        var invalidInterface = type
                               .GetInterfaces()
                               .Concat(new[] { type })
                               .Where(i => i.IsCustomCommandHandlerInterfaceType())
                               .FirstOrDefault(i => i.AllMethods().Count() > 1);

        if (invalidInterface != null)
        {
            throw new ArgumentException(
                $"command handler interface type '{invalidInterface.Name}' has extra methods; custom command handler interface types are not allowed to have any additional methods beside the '{nameof(ICommandHandler<object>.ExecuteCommand)}' inherited from '{typeof(ICommandHandler<>).Name}'");
        }
    }

    public static bool IsCustomCommandHandlerInterfaceType(this Type t) => t.IsInterface && t.GetInterfaces().Any(IsCommandHandlerInterfaceType);

    public static bool IsCustomCommandHandlerInterfaceType<TCommand, TResponse>(this Type t)
        where TCommand : class =>
        t.IsInterface && t.GetInterfaces().Any(i => i == typeof(ICommandHandler<TCommand>) || i == typeof(ICommandHandler<TCommand, TResponse>));

    public static bool IsCommandHandlerInterfaceType(this Type t) =>
        t.IsInterface && t.IsGenericType
                      && (t.GetGenericTypeDefinition() == typeof(ICommandHandler<>)
                          || t.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));
}
