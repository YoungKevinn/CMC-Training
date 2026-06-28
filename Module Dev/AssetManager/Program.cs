using System.Text.Json;
using AssetManager.Services;
using AssetManager.Storage;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON: camelCase → snake_case to match spec.
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    });

// DI: register storage as singleton (in-memory, lives for the app lifetime).
builder.Services.AddSingleton<IAssetStorage, MemoryStorage>();
builder.Services.AddScoped<IAssetService, AssetService>();

var app = builder.Build();

app.MapControllers();

// Match the port in the assignment spec.
app.Run("http://localhost:8080");
