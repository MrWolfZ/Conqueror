var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConquerorCQS().AddConquerorCQSTypesFromExecutingAssembly();
builder.Services.AddControllers().AddConquerorCQSHttpControllers();
builder.Services.FinalizeConquerorRegistrations();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
