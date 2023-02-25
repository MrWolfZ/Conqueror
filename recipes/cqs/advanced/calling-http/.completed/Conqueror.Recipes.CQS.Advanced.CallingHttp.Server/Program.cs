var builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddControllers()
       .AddConquerorCQSHttpControllers();

builder.Services
       .AddEndpointsApiExplorer()
       .AddSwaggerGen();

builder.Services
       .AddSingleton<CountersRepository>()
       .AddConquerorCQSTypesFromExecutingAssembly()

       // add all middlewares from the shared project
       .AddConquerorCQSTypesFromAssembly(typeof(DataAnnotationValidationCommandMiddleware).Assembly);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
