namespace Conqueror.Recipes.Messaging.CallingHttp.Client;

internal static class CounterServerTransportExtensions
{
    // in a real application this would be loaded from some configuration source,
    // e.g. by resolving IConfiguration from builder.ServiceProvider
    private static readonly Uri ServerAddress = new("http://localhost:5000");

    public static IHttpMessageSender<TMessage, TResponse> UseCounterServer<TMessage, TResponse>(
        this MessageSenderBuilder<TMessage, TResponse> builder)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
    {
        return builder.UseHttp(ServerAddress)
                      .WithHeaders(h => h.Add("my-header", "my-value"));
    }
}
