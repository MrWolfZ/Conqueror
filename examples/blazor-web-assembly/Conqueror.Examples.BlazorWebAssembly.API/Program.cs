using System.Runtime.CompilerServices;
using Conqueror;
using Conqueror.Examples.BlazorWebAssembly.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

[assembly: InternalsVisibleTo("Conqueror.Examples.BlazorWebAssembly.Tests")]

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddConquerorCQSHttpControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => o.DocInclusionPredicate((_, _) => true));
builder.Services.AddCors(c => c.AddPolicy("allow-all", b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().Build()));
builder.Services.AddTransient(p => p.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions);

builder.Services.AddApplication();
builder.Services.FinalizeConquerorRegistrations();

var app = builder.Build();

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("allow-all");
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseConqueror();
app.MapControllers();

app.Run();
