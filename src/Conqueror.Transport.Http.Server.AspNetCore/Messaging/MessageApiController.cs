using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Conqueror.Transport.Http.Server.AspNetCore.Messaging;

internal sealed class MessageApiController<TMessage, TResponse> : MessageApiControllerBase<TMessage, TResponse>
    where TMessage : class, IMessage<TResponse>, IHttpMessage<TMessage, TResponse>, IMessageTypes<TMessage, TResponse>
{
    public async Task<IActionResult> ExecuteMessage([FromBody] TMessage message)
    {
        var response = await HttpContext.HandleMessage(message).ConfigureAwait(false);

        if (typeof(TResponse) == typeof(UnitMessageResponse))
        {
            return StatusCode(TMessage.SuccessStatusCode);
        }

        return StatusCode(TMessage.SuccessStatusCode, response);
    }
}

internal sealed class MessageApiControllerWithoutPayload<TMessage, TResponse> : MessageApiControllerBase<TMessage, TResponse>
    where TMessage : class, IMessage<TResponse>, IHttpMessage<TMessage, TResponse>, IMessageTypes<TMessage, TResponse>
{
    public async Task<IActionResult> ExecuteMessage()
    {
        var response = await HttpContext.HandleMessage(TMessage.EmptyInstance!).ConfigureAwait(false);

        if (typeof(TResponse) == typeof(UnitMessageResponse))
        {
            return StatusCode(TMessage.SuccessStatusCode);
        }

        return StatusCode(TMessage.SuccessStatusCode, response);
    }
}

internal sealed class MessageApiControllerForGet<TMessage, TResponse> : MessageApiControllerBase<TMessage, TResponse>
    where TMessage : class, IMessage<TResponse>, IHttpMessage<TMessage, TResponse>, IMessageTypes<TMessage, TResponse>
{
    public async Task<IActionResult> ExecuteMessage([FromQuery] TMessage message)
    {
        var response = await HttpContext.HandleMessage(message).ConfigureAwait(false);

        if (typeof(TResponse) == typeof(UnitMessageResponse))
        {
            return StatusCode(TMessage.SuccessStatusCode);
        }

        return StatusCode(TMessage.SuccessStatusCode, response);
    }
}

[ApiController]
internal abstract class MessageApiControllerBase<TMessage, TResponse> : ControllerBase
    where TMessage : class, IMessage<TResponse>, IHttpMessage<TMessage, TResponse>, IMessageTypes<TMessage, TResponse>;
