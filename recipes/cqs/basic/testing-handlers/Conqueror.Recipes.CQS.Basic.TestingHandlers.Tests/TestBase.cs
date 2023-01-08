using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Conqueror.Recipes.CQS.Basic.TestingHandlers.Tests;

public abstract class TestBase : IDisposable
{
    private readonly IHost host;

    protected TestBase()
    {
        var hostBuilder = new HostBuilder().ConfigureServices(services => services.AddConquerorCQS()
                                                                                  .AddConquerorCQSTypesFromAssembly(typeof(IncrementCounterCommandHandler).Assembly)
                                                                                  .AddSingleton<CountersRepository>()
                                                                                  .FinalizeConquerorRegistrations());

        host = hostBuilder.StartAsync().Result;
    }

    public void Dispose()
    {
        host.Dispose();
    }

    protected T Resolve<T>()
        where T : notnull => host.Services.GetRequiredService<T>();
}
