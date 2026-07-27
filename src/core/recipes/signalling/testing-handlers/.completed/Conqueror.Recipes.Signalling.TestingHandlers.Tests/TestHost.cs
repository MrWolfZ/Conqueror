namespace Conqueror.Recipes.Signalling.TestingHandlers.Tests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

internal sealed class TestHost : IAsyncDisposable
{
    private readonly ServiceProvider serviceProvider;

    private TestHost()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices();

        services.Replace(ServiceDescriptor.Singleton(AdminNotificationServiceMock));

        serviceProvider = services.BuildServiceProvider();
    }

    public ISignalPublishers SignalPublishers =>
        serviceProvider.GetRequiredService<ISignalPublishers>();

    public IAdminNotificationService AdminNotificationServiceMock { get; } =
        Substitute.For<IAdminNotificationService>();

    public static TestHost Create() => new();

    public ValueTask DisposeAsync() => serviceProvider.DisposeAsync();

    public T Resolve<T>()
        where T : notnull => serviceProvider.GetRequiredService<T>();
}
