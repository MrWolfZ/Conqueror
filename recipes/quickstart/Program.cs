var builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddConquerorCQSTypesFromExecutingAssembly()
       .AddConquerorCQSLoggingMiddlewares();

builder.Services
       .AddControllers()
       .AddConquerorCQSHttpControllers();

builder.Services
       .AddEndpointsApiExplorer()
       .AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
