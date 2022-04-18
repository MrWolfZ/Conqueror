using System.Runtime.CompilerServices;
using Conqueror.Examples.BlazorWebAssembly.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

[assembly: InternalsVisibleTo("Conqueror.Examples.BlazorWebAssembly.Tests")]

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddConqueror();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(c => c.AddPolicy("allow-all", b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().Build()));
builder.Services.AddTransient(p => p.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions);

builder.Services.AddApplication();
builder.Services.ConfigureConqueror();

var app = builder.Build();

// TODO: replace this hack properly in Conqueror
app.Services.GetRequiredService<IStartupFilter>().Configure(_ => { })(app);

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("allow-all");
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
