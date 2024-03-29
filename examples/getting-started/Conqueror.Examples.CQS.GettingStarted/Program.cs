using System.Runtime.CompilerServices;
using Conqueror;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

[assembly: InternalsVisibleTo("Conqueror.Examples.CQS.GettingStarted.Tests")]

var builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddControllers()
       .AddConquerorCQSHttpControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient(p => p.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions);

builder.Services.AddConquerorCQSTypesFromExecutingAssembly();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseConqueror();
app.MapControllers();

app.Run();
