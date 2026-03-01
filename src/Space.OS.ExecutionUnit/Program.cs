using Magic.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Space.OS.ExecutionUnit;
using SpaceDb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MagicKernel>();
builder.Services.AddRocksDb(builder.Configuration);
builder.Services.AddSingleton<IExecutionUnitService, ExecutionUnitService>();
builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var kernel = scope.ServiceProvider.GetRequiredService<MagicKernel>();
    var disk = scope.ServiceProvider.GetRequiredService<RocksDbSpaceDisk>();
    kernel.Devices.Add(disk);
    await kernel.StartKernel();

    var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var spaceName = cfg["SpaceDisk:SpaceName"];
    if (!string.IsNullOrWhiteSpace(spaceName))
        disk.Configuration.SpaceName = spaceName.Trim();
    SetLong(cfg, "SpaceDisk:VertexSequenceIndex", v => disk.Configuration.VertexSequenceIndex = v);
    SetLong(cfg, "SpaceDisk:RelationSequenceIndex", v => disk.Configuration.RelationSequenceIndex = v);
    SetLong(cfg, "SpaceDisk:ShapeSequenceIndex", v => disk.Configuration.ShapeSequenceIndex = v);
}

app.MapControllers();
app.Run();

static void SetLong(IConfiguration cfg, string key, Action<long> setter)
{
    var raw = cfg[key];
    if (string.IsNullOrWhiteSpace(raw)) return;
    if (long.TryParse(raw, out var v))
        setter(v);
}
