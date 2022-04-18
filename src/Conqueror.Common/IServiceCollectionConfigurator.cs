using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Util
{
    internal interface IServiceCollectionConfigurator
    {
        void Configure(IServiceCollection services);
    }
}
