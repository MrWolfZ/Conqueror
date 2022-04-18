using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

// it makes sense for these types to be in the same file
#pragma warning disable SA1402

namespace Conqueror.CQS.Extensions.AspNetCore.Server
{
    [ApiController]
    public abstract class ConquerorCommandControllerBase<TCommand, TResponse> : ControllerBase
        where TCommand : class
    {
        protected async Task<TResponse> ExecuteCommand(TCommand command, CancellationToken cancellationToken)
        {
            var commandHandler = Request.HttpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
            return await commandHandler.ExecuteCommand(command, cancellationToken);
        }
    }

    [ApiController]
    public abstract class ConquerorCommandControllerBase<TCommand> : ControllerBase
        where TCommand : class
    {
        [ProducesResponseType(204)]
        protected async Task ExecuteCommand(TCommand command, CancellationToken cancellationToken)
        {
            var commandHandler = Request.HttpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand>>();
            await commandHandler.ExecuteCommand(command, cancellationToken);
        }
    }

    [ApiController]
    public abstract class ConquerorCommandWithoutPayloadControllerBase<TCommand, TResponse> : ControllerBase
        where TCommand : class, new()
    {
        protected async Task<TResponse> ExecuteCommand(CancellationToken cancellationToken)
        {
            var commandHandler = Request.HttpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
            return await commandHandler.ExecuteCommand(new(), cancellationToken);
        }
    }

    [ApiController]
    public abstract class ConquerorCommandWithoutPayloadControllerBase<TCommand> : ControllerBase
        where TCommand : class, new()
    {
        protected async Task ExecuteCommand(CancellationToken cancellationToken)
        {
            var commandHandler = Request.HttpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand>>();
            await commandHandler.ExecuteCommand(new(), cancellationToken);
        }
    }
}
