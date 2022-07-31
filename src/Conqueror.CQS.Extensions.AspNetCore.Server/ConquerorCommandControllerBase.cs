using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

// it makes sense for these types to be in the same file
#pragma warning disable SA1402

namespace Conqueror.CQS.Extensions.AspNetCore.Server
{
    [ApiController]
    public abstract class ConquerorCommandControllerBase<TCommand, TResponse> : CommandControllerBase
        where TCommand : class
    {
        protected async Task<TResponse> ExecuteCommand(TCommand command, CancellationToken cancellationToken)
        {
            return await ExecuteCommandWithContext(async () =>
            {
                var commandHandler = Request.HttpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
                return await commandHandler.ExecuteCommand(command, cancellationToken);
            });
        }
    }

    [ApiController]
    public abstract class ConquerorCommandControllerBase<TCommand> : CommandControllerBase
        where TCommand : class
    {
        [ProducesResponseType(204)]
        protected async Task ExecuteCommand(TCommand command, CancellationToken cancellationToken)
        {
            _ = await ExecuteCommandWithContext(async () =>
            {
                var commandHandler = Request.HttpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand>>();
                await commandHandler.ExecuteCommand(command, cancellationToken);
                return UnitCommandResponse.Instance;
            });
        }
    }

    [ApiController]
    public abstract class ConquerorCommandWithoutPayloadControllerBase<TCommand, TResponse> : CommandControllerBase
        where TCommand : class, new()
    {
        protected async Task<TResponse> ExecuteCommand(CancellationToken cancellationToken)
        {
            return await ExecuteCommandWithContext(async () =>
            {
                var commandHandler = Request.HttpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
                return await commandHandler.ExecuteCommand(new(), cancellationToken);
            });
        }
    }

    [ApiController]
    public abstract class ConquerorCommandWithoutPayloadControllerBase<TCommand> : CommandControllerBase
        where TCommand : class, new()
    {
        protected async Task ExecuteCommand(CancellationToken cancellationToken)
        {
            _ = await ExecuteCommandWithContext(async () =>
            {
                var commandHandler = Request.HttpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand>>();
                await commandHandler.ExecuteCommand(new(), cancellationToken);
                return UnitCommandResponse.Instance;
            });
        }
    }

    [TypeFilter(typeof(ExceptionHandlerFilter))]
    public abstract class CommandControllerBase : ControllerBase
    {
        protected async Task<TResponse> ExecuteCommandWithContext<TResponse>(Func<Task<TResponse>> executeFn)
        {
            var context = Request.HttpContext.RequestServices.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

            if (Request.Headers.TryGetValue(HttpConstants.ConquerorContextHeaderName, out var values))
            {
                try
                {
                    var parsedValue = ContextValueFormatter.Parse(values.AsEnumerable());

                    foreach (var (key, value) in parsedValue)
                    {
                        context.Items[key] = value;
                    }
                }
                catch
                {
                    throw new BadContextException();
                }
            }

            var response = await executeFn();

            if (context.Items.Count > 0)
            {
                Response.Headers.Add(HttpConstants.ConquerorContextHeaderName, ContextValueFormatter.Format(context.Items));
            }

            return response;
        }

        private sealed class ExceptionHandlerFilter : IExceptionFilter
        {
            public void OnException(ExceptionContext context)
            {
                if (context.Exception is not BadContextException)
                {
                    return;
                }

                context.Result = new BadRequestObjectResult("invalid command context header");
            }
        }

#pragma warning disable
        private sealed class BadContextException : Exception
#pragma warning restore
        {
        }
    }
}
