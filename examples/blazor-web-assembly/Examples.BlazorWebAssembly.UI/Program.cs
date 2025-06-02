using Examples.BlazorWebAssembly.UI;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
       .AddConquerorHttpClient()
       .AddSignalHandler<ChatEntryBroadcastedHandler>(ServiceLifetime.Singleton);

await builder.Build().RunAsync();
