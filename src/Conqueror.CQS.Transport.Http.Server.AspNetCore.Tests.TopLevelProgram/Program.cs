using System.Runtime.CompilerServices;
using Conqueror;

[assembly: InternalsVisibleTo("Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests")]

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
       .AddControllers()
       .AddConquerorCQSHttpControllers();

builder.Services.AddConquerorCQSTypesFromExecutingAssembly();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseConqueror();
app.MapControllers();

app.Run();
