global using Conqueror.Examples.BlazorWebAssembly.Contracts;
using Conqueror.Examples.BlazorWebAssembly.UI;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
       .AddConquerorHttpClients()
       .ConfigureDefaultHttpClientOptions(o =>
       {
           o.HttpClientFactory = _ =>
           {
               var baseAddressFromConfig = builder.Configuration["ApiBaseAddress"];
               var baseAddress = string.IsNullOrWhiteSpace(baseAddressFromConfig) ? builder.HostEnvironment.BaseAddress : baseAddressFromConfig;
               return new() { BaseAddress = new(baseAddress) };
           };
       })
       .AddQueryHttpClient<IGetSharedCounterValueQueryHandler>()
       .AddCommandHttpClient<IIncrementSharedCounterValueCommandHandler>();

await builder.Build().RunAsync();
