﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.CQS.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class InMemoryCommandTransport : ICommandTransport
    {
        private readonly IServiceProvider serviceProvider;
        private readonly Type handlerType;

        public InMemoryCommandTransport(IServiceProvider serviceProvider, Type handlerType)
        {
            this.serviceProvider = serviceProvider;
            this.handlerType = handlerType;
        }

        public async Task<TResponse> Execute<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
            where TCommand : class
        {
            var handler = serviceProvider.GetRequiredService(handlerType);

            if (handler is ICommandHandler<TCommand, TResponse> h)
            {
                return await h.ExecuteCommand(command, cancellationToken);
            }

            await ((ICommandHandler<TCommand>)handler).ExecuteCommand(command, cancellationToken);
            return (TResponse)(object)UnitCommandResponse.Instance;
        }
    }
}
