namespace Conqueror;

public static class HttpMessageSenderBuilderExtensions
{
    public static IHttpMessageSender<TMessage, TResponse> UseHttp<TMessage, TResponse>(
        this MessageSenderBuilder<TMessage, TResponse> builder,
        Uri baseAddress
    )
        where TMessage : class, IHttpMessage<TMessage, TResponse>
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(baseAddress);

        if (
            builder.ServiceProvider.GetService(typeof(IHttpMessageSenderFactory))
            is not IHttpMessageSenderFactory senderFactory
        )
        {
            throw new InvalidOperationException(
                $"could not resolve '{typeof(IHttpMessageSenderFactory)}'; did you forget to add the Conqueror HTTP client to the service collection?"
            );
        }

        return senderFactory.Create<TMessage, TResponse>(baseAddress);
    }
}
