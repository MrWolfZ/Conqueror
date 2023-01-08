var builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddEndpointsApiExplorer()
       .AddSwaggerGen();

builder.Services
       .AddConquerorCQS()
       .AddConquerorCQSTypesFromExecutingAssembly()
       .AddConquerorCQSLoggingMiddlewares();

builder.Services.AddControllers().AddConquerorCQSHttpControllers();
builder.Services.FinalizeConquerorRegistrations();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
