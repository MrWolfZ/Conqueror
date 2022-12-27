using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests")]

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddConquerorCQSHttpControllers();

builder.Services.AddConquerorCQS();
builder.Services.AddConquerorCQSTypesFromExecutingAssembly();
builder.Services.ConfigureConqueror();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapControllers();

app.Run();
