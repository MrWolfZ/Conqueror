using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Conqueror.Recipes.CQS.Basic.TestingHandlers.Tests;

public abstract class TestBase : IDisposable
{
    private readonly ServiceProvider serviceProvider;

    protected TestBase()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices();

        services.Replace(ServiceDescriptor.Singleton(AdminNotificationServiceMock.Object));

        serviceProvider = services.BuildServiceProvider();
    }

    protected Mock<IAdminNotificationService> AdminNotificationServiceMock { get; } = new();

    public void Dispose()
    {
        serviceProvider.Dispose();
    }

    protected T Resolve<T>()
        where T : notnull => serviceProvider.GetRequiredService<T>();
}
