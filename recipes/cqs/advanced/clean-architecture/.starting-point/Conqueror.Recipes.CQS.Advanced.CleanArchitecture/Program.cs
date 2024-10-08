using Conqueror.Recipes.CQS.Advanced.CleanArchitecture;

var builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddControllers()
       .AddConquerorCQSHttpControllers();

builder.Services
       .AddEndpointsApiExplorer()
       .AddSwaggerGen();

builder.Services
       .AddSingleton<CountersRepository>()
       .AddSingleton<UserHistoryRepository>()
       .AddConquerorCQSTypesFromExecutingAssembly();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseConqueror();
app.MapControllers();

app.Run();
