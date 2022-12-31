using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Common
{
    internal interface IServiceCollectionConfigurator
    {
        int ConfigurationPhase { get; }

        void Configure(IServiceCollection services);
    }
}
