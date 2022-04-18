// ReSharper disable once CheckNamespace (used as part of service registration, which by convention should be in this namespace)

namespace Microsoft.Extensions.DependencyInjection
{
    public interface IConquerorHttpClientsBuilder
    {
        /// <summary>
        ///     Gets the <see cref="IServiceCollection" /> where Conqueror services are configured.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
