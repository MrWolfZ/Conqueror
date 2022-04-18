using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Conqueror.CQS
{
    internal sealed class CommandHandlerMetadata
    {
        public CommandHandlerMetadata(Type commandType, Type? responseType, Type handlerType)
        {
            CommandType = commandType;
            ResponseType = responseType;
            HandlerType = handlerType;
            MiddlewareConfigurationAttributes = GetConfigurationAttribute(handlerType).ToDictionary(a => a.GetType());
        }

        public Type CommandType { get; }

        public Type? ResponseType { get; }

        public Type HandlerType { get; }

        public IReadOnlyDictionary<Type, CommandMiddlewareConfigurationAttribute> MiddlewareConfigurationAttributes { get; }

        public bool TryGetMiddlewareConfigurationAttribute<TConfiguration>([MaybeNullWhen(false)] out TConfiguration attribute)
            where TConfiguration : CommandMiddlewareConfigurationAttribute, ICommandMiddlewareConfiguration<ICommandMiddleware<TConfiguration>>
        {
            var success = MiddlewareConfigurationAttributes.TryGetValue(typeof(TConfiguration), out var a);
            attribute = a as TConfiguration;
            return success && attribute != null;
        }

        private static IEnumerable<CommandMiddlewareConfigurationAttribute> GetConfigurationAttribute(Type handlerType)
        {
            var executeMethod = handlerType.GetMethod(nameof(ICommandHandler<object, object>.ExecuteCommand), BindingFlags.Instance | BindingFlags.Public);
            return executeMethod?.GetCustomAttributes().OfType<CommandMiddlewareConfigurationAttribute>() ?? Enumerable.Empty<CommandMiddlewareConfigurationAttribute>();
        }
    }
}
