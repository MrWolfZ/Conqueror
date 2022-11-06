using Conqueror.CQS.Transport.Http.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Conqueror.Examples.BlazorWebAssembly.UI;

public static class ConquerorTransportClientBuilderExtensions
{
    public static ICommandTransportClient UseHttpApi(this ICommandTransportClientBuilder builder)
    {
        return builder.UseHttp(GetApiBaseAddress(builder.ServiceProvider));
    }
    
    public static IQueryTransportClient UseHttpApi(this IQueryTransportClientBuilder builder)
    {
        return builder.UseHttp(GetApiBaseAddress(builder.ServiceProvider));
    }

    private static Uri GetApiBaseAddress(IServiceProvider serviceProvider)
    {
        var baseAddressFromConfig = serviceProvider.GetRequiredService<IConfiguration>()["ApiBaseAddress"];
        var baseAddress = string.IsNullOrWhiteSpace(baseAddressFromConfig) ? serviceProvider.GetRequiredService<IWebAssemblyHostEnvironment>().BaseAddress : baseAddressFromConfig;
        return new(baseAddress);
    }
}
