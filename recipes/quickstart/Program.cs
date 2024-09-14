using Conqueror;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConquerorCQSTypesFromExecutingAssembly();

builder.Services
       .AddControllers()
       .AddConquerorCQSHttpControllers();

builder.Services
       .AddEndpointsApiExplorer()
       .AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseConqueror();
app.MapControllers();

app.Run();
