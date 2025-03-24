﻿using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Conqueror.Transport.Http.Server.AspNetCore.Messaging;

internal sealed class MessageApiController<TMessage, TResponse> : MessageApiControllerBase<TMessage, TResponse>
    where TMessage : class, IHttpMessage<TMessage, TResponse>
{
    public async Task<IActionResult> ExecuteMessage([FromBody] TMessage message, CancellationToken cancellationToken)
    {
        var response = await HttpContext.GetMessageClient(TMessage.T)
                                        .Handle(message, cancellationToken)
                                        .ConfigureAwait(false);

        if (typeof(TResponse) == typeof(UnitMessageResponse))
        {
            return StatusCode(TMessage.SuccessStatusCode);
        }

        return StatusCode(TMessage.SuccessStatusCode, response);
    }
}

internal sealed class MessageApiControllerWithoutPayload<TMessage, TResponse> : MessageApiControllerBase<TMessage, TResponse>
    where TMessage : class, IHttpMessage<TMessage, TResponse>
{
    public async Task<IActionResult> ExecuteMessage(CancellationToken cancellationToken)
    {
        var response = await HttpContext.GetMessageClient(TMessage.T)
                                        .Handle(TMessage.EmptyInstance!, cancellationToken)
                                        .ConfigureAwait(false);

        if (typeof(TResponse) == typeof(UnitMessageResponse))
        {
            return StatusCode(TMessage.SuccessStatusCode);
        }

        return StatusCode(TMessage.SuccessStatusCode, response);
    }
}

internal sealed class MessageApiControllerForGet<TMessage, TResponse> : MessageApiControllerBase<TMessage, TResponse>
    where TMessage : class, IHttpMessage<TMessage, TResponse>
{
    public async Task<IActionResult> ExecuteMessage([FromQuery] TMessage message, CancellationToken cancellationToken)
    {
        var response = await HttpContext.GetMessageClient(TMessage.T)
                                        .Handle(message, cancellationToken)
                                        .ConfigureAwait(false);

        if (typeof(TResponse) == typeof(UnitMessageResponse))
        {
            return StatusCode(TMessage.SuccessStatusCode);
        }

        return StatusCode(TMessage.SuccessStatusCode, response);
    }
}

[ApiController]
internal abstract class MessageApiControllerBase<TMessage, TResponse> : ControllerBase
    where TMessage : class, IHttpMessage<TMessage, TResponse>;
