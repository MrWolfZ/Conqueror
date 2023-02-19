using Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Counters.Application;
using Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Counters.Infrastructure;
using Conqueror.Recipes.CQS.Advanced.CleanArchitecture.UserHistory.Application;
using Conqueror.Recipes.CQS.Advanced.CleanArchitecture.UserHistory.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddControllers()
       .AddConquerorCQSHttpControllers();

builder.Services
       .AddEndpointsApiExplorer()
       .AddSwaggerGen();

builder.Services
       .AddCountersApplication()
       .AddCountersInfrastructure()
       .AddUserHistoryApplication()
       .AddUserHistoryInfrastructure()
       .FinalizeConquerorRegistrations();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
