using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.Extensions.AspNetCore.Client
{
    internal sealed class ConquerorHttpClientsBuilder : IConquerorHttpClientsBuilder
    {
        public ConquerorHttpClientsBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }
}
