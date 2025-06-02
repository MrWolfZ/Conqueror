using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Examples.BlazorWebAssembly.UI;

public static class MessageSenderBuilderExtensions
{
    public static IMessageSender<TMessage, TResponse> UseHttpApi<TMessage, TResponse>(this IMessageSenderBuilder<TMessage, TResponse> builder)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
    {
        return builder.UseHttp(builder.ServiceProvider.GetApiBaseAddress());
    }

    public static TIHandler WithHttpApiTransport<TMessage, TResponse, TIHandler>(this IMessageHandler<TMessage, TResponse, TIHandler> builder)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
    {
        return builder.WithTransport(b => b.UseHttpApi());
    }
}
