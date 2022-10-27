global using Conqueror.Examples.BlazorWebAssembly.Contracts;
using System.Text.Json;
using Conqueror.Examples.BlazorWebAssembly.SharedMiddlewares;
using Conqueror.Examples.BlazorWebAssembly.UI;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
       .AddConquerorCQS()
       .AddConquerorCQSTypesFromExecutingAssembly()
       .AddConquerorCQSTypesFromAssembly(typeof(CommandTimeoutMiddleware).Assembly)
       .AddConquerorCQSHttpClientServices()
       .AddConquerorQueryClient<IGetSharedCounterValueQueryHandler>(b => b.UseHttpApi(), pipeline => pipeline.UseDefaultHttpPipeline())
       .AddConquerorCommandClient<IIncrementSharedCounterValueCommandHandler>(b => b.UseHttpApi(), pipeline => pipeline.UseDefaultHttpPipeline())
       .AddSingleton(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
       .ConfigureConqueror();

await builder.Build().RunAsync();
