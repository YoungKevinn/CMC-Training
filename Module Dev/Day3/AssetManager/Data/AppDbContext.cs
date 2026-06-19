using AssetManager.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Asset> Assets { get; set; }
    public DbSet<ScanJob> ScanJobs { get; set; }
    public DbSet<ScanResult> ScanResults { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Asset>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.Type);
            e.HasIndex(a => a.Status);
        });

        modelBuilder.Entity<ScanJob>(e =>
        {
            e.HasKey(j => j.Id);
            e.HasIndex(j => j.AssetId);
            e.HasIndex(j => j.Status);
        });

        modelBuilder.Entity<ScanResult>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.JobId);
            e.HasIndex(r => r.AssetId);
            e.HasIndex(r => r.ScanType);
        });
    }
}
