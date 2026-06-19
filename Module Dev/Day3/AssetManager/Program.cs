using System.Text.Json;
using System.Threading.Channels;
using AssetManager.Data;
using AssetManager.Scanners;
using AssetManager.Services;
using AssetManager.Storage;
using AssetManager.Workers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// JSON: use snake_case for all API responses
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        opts.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// CORS — allow all origins so the frontend can connect (Bài 4)
builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// Database: SQLite via EF Core (Bài 1)
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Storage layer
builder.Services.AddScoped<IAssetStorage, EfAssetStorage>();
builder.Services.AddScoped<IScanRepository, EfScanRepository>();

// Business logic
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IScanService, ScanService>();

// Scanners — registered as singleton since they hold no mutable state
// Existing passive scanners
builder.Services.AddSingleton<IScanner, DnsScanner>();
builder.Services.AddSingleton<IScanner, WhoisScanner>();
builder.Services.AddSingleton<IScanner, SubdomainScanner>();
builder.Services.AddSingleton<IScanner, CertTransScanner>();
builder.Services.AddSingleton<IScanner, AsnScanner>();
// New scanners added in Day 3
builder.Services.AddSingleton<IScanner, IpScanner>();
builder.Services.AddSingleton<IScanner, PortScanner>();
builder.Services.AddSingleton<IScanner, SslScanner>();
builder.Services.AddSingleton<IScanner, TechScanner>();

builder.Services.AddSingleton<ScannerFactory>();

// HttpClient for scanners that call external APIs
builder.Services.AddHttpClient("scanner", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; EASM-Scanner/1.0)");
});

// Channel used to pass job IDs from ScanService to ScanWorker
builder.Services.AddSingleton(_ => Channel.CreateUnbounded<string>(new UnboundedChannelOptions
{
    SingleReader = true  // only ScanWorker reads
}));

// Background worker that processes scan jobs
builder.Services.AddHostedService<ScanWorker>();

var app = builder.Build();

// Ensure DB schema is created on first run
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors("AllowAll");
app.MapControllers();

app.Run("http://localhost:8080");
