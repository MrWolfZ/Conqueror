using System;
using System.Collections.Generic;
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
            MiddlewareConfigurationAttributes = GetConfigurationAttribute(handlerType).ToList();
        }

        public Type CommandType { get; }

        public Type? ResponseType { get; }

        public Type HandlerType { get; }

        public IReadOnlyCollection<CommandMiddlewareConfigurationAttribute> MiddlewareConfigurationAttributes { get; }

        private IEnumerable<CommandMiddlewareConfigurationAttribute> GetConfigurationAttribute(Type handlerType)
        {
            var executeCommandMethodName = nameof(ICommandHandler<object, object>.ExecuteCommand);
            var executeMethod = handlerType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                           .FirstOrDefault(m => m.Name == executeCommandMethodName && m.GetParameters().FirstOrDefault()?.ParameterType == CommandType);

            return executeMethod?.GetCustomAttributes().OfType<CommandMiddlewareConfigurationAttribute>() ?? Enumerable.Empty<CommandMiddlewareConfigurationAttribute>();
        }
    }
}
