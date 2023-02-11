using Conqueror.Recipes.CQS.Advanced.ExposingViaHttp;

var builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddControllers()
       .AddConquerorCQSHttpControllers(o => o.CommandPathConvention = new CustomHttpCommandPathConvention());

builder.Services
       .AddEndpointsApiExplorer()
       .AddSwaggerGen(c => c.DocInclusionPredicate((_, _) => true));

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
