using Conqueror.Recipes.CQS.Advanced.ExposingViaHttp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services
       .AddEndpointsApiExplorer()
       .AddSwaggerGen();

builder.Services
       .AddSingleton<CountersRepository>()
       .AddConquerorCQSTypesFromExecutingAssembly();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
