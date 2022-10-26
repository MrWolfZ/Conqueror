﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandPipeline
    {
        private readonly CommandContextAccessor commandContextAccessor;
        private readonly ConquerorContextAccessor conquerorContextAccessor;
        private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration, ICommandMiddlewareInvoker Invoker)> middlewares;

        public CommandPipeline(CommandContextAccessor commandContextAccessor,
                               ConquerorContextAccessor conquerorContextAccessor,
                               List<(Type MiddlewareType, object? MiddlewareConfiguration, ICommandMiddlewareInvoker Invoker)> middlewares)
        {
            this.commandContextAccessor = commandContextAccessor;
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.middlewares = middlewares;
        }

        public async Task<TResponse> Execute<TCommand, TResponse>(IServiceProvider serviceProvider,
                                                                  TCommand initialCommand,
                                                                  Func<ICommandTransportBuilder, ICommandTransport> transportFactory,
                                                                  CancellationToken cancellationToken)
            where TCommand : class
        {
            var commandContext = new DefaultCommandContext(initialCommand);

            commandContextAccessor.CommandContext = commandContext;

            using var conquerorContext = conquerorContextAccessor.GetOrCreate();
            
            var transportBuilder = new CommandTransportBuilder(serviceProvider);

            try
            {
                return await ExecuteNextMiddleware(0, initialCommand, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                commandContextAccessor.ClearContext();
            }

            async Task<TResponse> ExecuteNextMiddleware(int index, TCommand command, CancellationToken token)
            {
                commandContext.SetCommand(command);

                if (index >= middlewares.Count)
                {
                    var transport = transportFactory(transportBuilder);
                    var responseFromHandler = await transport.ExecuteCommand<TCommand, TResponse>(command, token).ConfigureAwait(false);
                    commandContext.SetResponse(responseFromHandler);
                    return responseFromHandler;
                }

                var (_, middlewareConfiguration, invoker) = middlewares[index];
                var response = await invoker.Invoke(command, (c, t) => ExecuteNextMiddleware(index + 1, c, t), middlewareConfiguration, serviceProvider, token).ConfigureAwait(false);
                commandContext.SetResponse(response);
                return response;
            }
        }
    }
}
