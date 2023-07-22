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
       .AddConquerorCQSTypesFromExecutingAssembly();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseConqueror();
app.MapControllers();

app.Run();
