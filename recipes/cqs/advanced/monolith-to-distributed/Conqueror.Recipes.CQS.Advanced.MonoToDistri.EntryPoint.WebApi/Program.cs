using Conqueror;
using Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.Application;
using Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.Infrastructure;
using Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Application;
using Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Infrastructure;

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
       .AddUserHistoryInfrastructure();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseConqueror();
app.MapControllers();

app.Run();
