global using Conqueror.Examples.BlazorWebAssembly.Contracts;
using System.Text.Json;
using Conqueror.Examples.BlazorWebAssembly.UI;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
       .AddConquerorCQSTypesFromExecutingAssembly()
       .AddConquerorCQSHttpClientServices()
       .AddConquerorQueryClient<IGetSharedCounterValueQueryHandler>(b => b.UseHttpApi())
       .AddConquerorCommandClient<IIncrementSharedCounterValueCommandHandler>(b => b.UseHttpApi())
       .AddSingleton(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

await builder.Build().RunAsync();
