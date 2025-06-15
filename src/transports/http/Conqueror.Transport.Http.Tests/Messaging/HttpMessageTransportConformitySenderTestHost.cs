using Conqueror.Transport.ConformityTests.Messaging;

namespace Conqueror.Transport.Http.Tests.Messaging;

public sealed class HttpMessageTransportConformitySenderTestHost : IMessageTransportConformitySenderTestHost
{
    private readonly ServiceProvider serviceProvider;

    private HttpMessageTransportConformitySenderTestHost(ServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public IMessageSenders MessageSenders => serviceProvider.GetRequiredService<IMessageSenders>();

    public IConquerorContextAccessor ConquerorContextAccessor => serviceProvider.GetRequiredService<IConquerorContextAccessor>();

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "false positive, service provider is disposed by the test host")]
    public static HttpMessageTransportConformitySenderTestHost CreateSenderHost(
        HttpMessageTransportConformityTestHost host,
        HttpMessageConformityTestCase testCase,
        Func<object, ConquerorContext, CancellationToken, Task>? sendCallback)
    {
        var services = new ServiceCollection();

        _ = services.AddConquerorHttpClient()
                    .AddSingleton(host.Logger)
                    .AddSingleton<HttpClient>(new CustomHttpClient(host))
                    .AddTransient(typeof(HttpMessageTestCases.TestMessageMiddleware<,>));

        if (sendCallback is not null)
        {
            _ = services.AddSingleton(sendCallback);
        }

        testCase.RegisterClientServices(services);

        var serviceProvider = services.BuildServiceProvider();

        var receiverHost = new HttpMessageTransportConformitySenderTestHost(serviceProvider);

        return receiverHost;
    }

    public T Resolve<T>()
        where T : notnull
        => serviceProvider.GetRequiredService<T>();

    public async ValueTask DisposeAsync()
    {
        await serviceProvider.DisposeAsync();
    }

    private sealed class CustomHttpClient : HttpClient
    {
        private readonly HttpMessageTransportConformityTestHost host;

        public CustomHttpClient(HttpMessageTransportConformityTestHost host)
        {
            this.host = host;

            BaseAddress = host.ReceiverHost.HttpClient.BaseAddress;
        }

        public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => host.ReceiverHost.HttpClient.SendAsync(request, cancellationToken);
    }
}
