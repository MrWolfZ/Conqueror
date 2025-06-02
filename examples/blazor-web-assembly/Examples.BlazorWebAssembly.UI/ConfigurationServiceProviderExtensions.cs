using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Examples.BlazorWebAssembly.UI;

public static class ConfigurationServiceProviderExtensions
{
    public static Uri GetApiBaseAddress(this IServiceProvider serviceProvider)
    {
        var baseAddressFromConfig = serviceProvider.GetRequiredService<IConfiguration>()["ApiBaseAddress"];
        var baseAddress = string.IsNullOrWhiteSpace(baseAddressFromConfig)
            ? serviceProvider.GetRequiredService<IWebAssemblyHostEnvironment>().BaseAddress
            : baseAddressFromConfig;

        return new(baseAddress);
    }
}
