using Conqueror.Transport.ConformityTests.Messaging;
using Microsoft.AspNetCore.Routing;

namespace Conqueror.Transport.Http.Tests.Messaging;

public abstract class HttpMessageConformityTestCase : IMessageTransportConformityTestCase<HttpMessageTransportConformityTestHost>
{
    public required string Name { get; init; }

    public required Action<IServiceCollection> RegisterHandler { get; init; }

    public Action<IServiceCollection>? RegisterOnClient { get; init; }

    public required Action<IEndpointRouteBuilder> MapEndpoints { get; init; }

    public required Func<IMessageSenders, CancellationToken, Task<IReadOnlyCollection<object>>> SendMessages { get; init; }

    public virtual HttpMessageTransportConformityTestHost CreateTestHost() => HttpMessageTransportConformityTestHost.Create(this);

    Task<IReadOnlyCollection<object>> IMessageTransportConformityTestCase<HttpMessageTransportConformityTestHost>.SendMessages(
        IMessageSenders messageSenders,
        CancellationToken cancellationToken)
        => SendMessages(messageSenders, cancellationToken);

    public virtual void RegisterServerServices(IServiceCollection services)
    {
    }

    public virtual void RegisterClientServices(IServiceCollection services)
    {
        RegisterOnClient?.Invoke(services);
    }

    public virtual void ConfigureReceiver(HttpMessageTransportConformityTestHost host, IHttpMessageReceiver receiver)
    {
    }
}
