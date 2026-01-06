namespace Conqueror.Recipes.Messaging.TestingHandlers.Tests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public abstract class TestBase
{
    private readonly ServiceProvider serviceProvider;

    protected TestBase()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices();

        services.Replace(ServiceDescriptor.Singleton(AdminNotificationServiceMock));

        serviceProvider = services.BuildServiceProvider();
    }

    protected IMessageSenders MessageSenders => serviceProvider.GetRequiredService<IMessageSenders>();

    protected IAdminNotificationService AdminNotificationServiceMock { get; } =
        Substitute.For<IAdminNotificationService>();

    [TearDown]
    public void TearDown()
    {
        serviceProvider.Dispose();
    }

    protected T Resolve<T>()
        where T : notnull => serviceProvider.GetRequiredService<T>();
}
