global using Conqueror.Examples.BlazorWebAssembly.Contracts;
using Conqueror.CQS.Extensions.AspNetCore.Client;
using Conqueror.Examples.BlazorWebAssembly.UI;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
       .AddConquerorCqsHttpClientServices()
       .AddConquerorQueryHttpClient<IGetSharedCounterValueQueryHandler>(GetConquerorHttpClientAddress)
       .AddConquerorCommandClient<IIncrementSharedCounterValueCommandHandler>(b => b.UseHttp(GetConquerorHttpClientAddress(b.ServiceProvider)));

await builder.Build().RunAsync();

Uri GetConquerorHttpClientAddress(IServiceProvider serviceProvider)
{
    var baseAddressFromConfig = serviceProvider.GetRequiredService<IConfiguration>()["ApiBaseAddress"];
    var baseAddress = string.IsNullOrWhiteSpace(baseAddressFromConfig) ? serviceProvider.GetRequiredService<IWebAssemblyHostEnvironment>().BaseAddress : baseAddressFromConfig;
    return new(baseAddress);
}
