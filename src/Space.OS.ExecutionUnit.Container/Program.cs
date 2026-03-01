using Space.OS.ExecutionUnit.Container;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ContainerOptions>(builder.Configuration.GetSection(ContainerOptions.SectionName));
builder.Services.AddSingleton<IExecutionUnitHost, ExecutionUnitHost>();
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
