using Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Application;
using Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddControllers()
       .AddConquerorCQSHttpControllers();

builder.Services
       .AddEndpointsApiExplorer()
       .AddSwaggerGen();

builder.Services
       .AddApplication()
       .AddInfrastructure()
       .FinalizeConquerorRegistrations();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
