namespace Conqueror.Recipes.CQS.Basics.TestingMiddlewares.Tests;

public abstract class TestBase : IDisposable
{
    private readonly Lazy<ServiceProvider> serviceProviderLazy;

    protected TestBase()
    {
        serviceProviderLazy = new(BuildServiceProvider);
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    public void Dispose()
    {
        if (serviceProviderLazy.IsValueCreated)
        {
            serviceProviderLazy.Value.Dispose();
        }
    }

    protected T Resolve<T>()
        where T : notnull => serviceProviderLazy.Value.GetRequiredService<T>();

    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices();

        ConfigureServices(services);

        return services.FinalizeConquerorRegistrations().BuildServiceProvider();
    }
}
