using Conqueror.Recipes.CQS.Advanced.TestingHttp;

var builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddControllers()
       .AddConquerorCQSHttpControllers();

builder.Services
       .AddEndpointsApiExplorer()
       .AddSwaggerGen();

builder.Services
       .AddSingleton<CountersRepository>()
       .AddConquerorCQS()
       .AddConquerorCQSTypesFromExecutingAssembly()
       .FinalizeConquerorRegistrations();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
