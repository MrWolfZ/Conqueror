namespace Conqueror.Recipes.Messaging.TestingCallingHttp.Client;

internal static class CounterServerTransportExtensions
{
    public static IHttpMessageSender<TMessage, TResponse> UseCounterServer<TMessage, TResponse>(
        this MessageSenderBuilder<TMessage, TResponse> builder)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
    {
        // the HTTP client is resolved from the app's services (with the server address as its
        // base address) so that tests can replace it, e.g. with a test server's client
        var httpClient = builder.ServiceProvider.GetRequiredService<HttpClient>();

        return builder.UseHttp(httpClient.BaseAddress!)
                      .WithHttpClient(httpClient)
                      .WithHeaders(h => h.Add("my-header", "my-value"));
    }
}
