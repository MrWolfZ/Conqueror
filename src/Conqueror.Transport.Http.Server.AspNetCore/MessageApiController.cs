using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Transport.Http.Server.AspNetCore;

[ApiController]
internal sealed class MessageApiController<TMessage, TResponse> : ControllerBase
    where TMessage : class, IMessage<TResponse>, IHttpMessage<TMessage>
{
    public Task<TResponse> ExecuteMessage(TMessage message)
    {
        return HttpContext.RequestServices
                          .GetRequiredService<IMessageClients>()
                          .ForMessageType<TMessage, TResponse>()
                          .Handle(message);
    }
}
