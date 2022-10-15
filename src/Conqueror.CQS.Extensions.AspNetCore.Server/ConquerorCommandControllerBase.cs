using System.Threading;
using System.Threading.Tasks;
using Conqueror.CQS.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

// it makes sense for these types to be in the same file
#pragma warning disable SA1402

namespace Conqueror.CQS.Extensions.AspNetCore.Server
{
    [ApiController]
    public abstract class ConquerorCommandControllerBase<TCommand, TResponse> : ConquerorControllerBase
        where TCommand : class
    {
        protected async Task<TResponse> ExecuteCommand(TCommand command, CancellationToken cancellationToken)
        {
            return await ExecuteWithContext(async () =>
            {
                var commandHandler = Request.HttpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
                return await commandHandler.ExecuteCommand(command, cancellationToken);
            });
        }
    }

    [ApiController]
    public abstract class ConquerorCommandControllerBase<TCommand> : ConquerorControllerBase
        where TCommand : class
    {
        [ProducesResponseType(204)]
        protected async Task ExecuteCommand(TCommand command, CancellationToken cancellationToken)
        {
            _ = await ExecuteWithContext(async () =>
            {
                var commandHandler = Request.HttpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand>>();
                await commandHandler.ExecuteCommand(command, cancellationToken);
                return UnitCommandResponse.Instance;
            });
        }
    }

    [ApiController]
    public abstract class ConquerorCommandWithoutPayloadControllerBase<TCommand, TResponse> : ConquerorControllerBase
        where TCommand : class, new()
    {
        protected async Task<TResponse> ExecuteCommand(CancellationToken cancellationToken)
        {
            return await ExecuteWithContext(async () =>
            {
                var commandHandler = Request.HttpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
                return await commandHandler.ExecuteCommand(new(), cancellationToken);
            });
        }
    }

    [ApiController]
    public abstract class ConquerorCommandWithoutPayloadControllerBase<TCommand> : ConquerorControllerBase
        where TCommand : class, new()
    {
        protected async Task ExecuteCommand(CancellationToken cancellationToken)
        {
            _ = await ExecuteWithContext(async () =>
            {
                var commandHandler = Request.HttpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand>>();
                await commandHandler.ExecuteCommand(new(), cancellationToken);
                return UnitCommandResponse.Instance;
            });
        }
    }
}
