using System.Runtime.CompilerServices;
using Examples.BlazorWebAssembly.API.Chat;

[assembly: InternalsVisibleTo("Examples.BlazorWebAssembly.Tests")]

var builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddMessageHandlersFromAssembly(typeof(Program).Assembly)
       .AddMessageEndpoints()
       .AddSingleton<ChatRepository>()
       .AddSwaggerGen(o => o.DocInclusionPredicate((_, _) => true))
       .AddCors(c => c.AddPolicy("allow-all", b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().Build()))
       .AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("allow-all");
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseConquerorWellKnownErrorHandling();
app.MapMessageEndpoints();

app.Run();
