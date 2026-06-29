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
        // MySQL cannot index unbounded TEXT columns, so indexed string
        // columns need an explicit max length (EF Core defaults to longtext).
        modelBuilder.Entity<Asset>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasMaxLength(64);
            e.Property(a => a.Type).HasMaxLength(50);
            e.Property(a => a.Status).HasMaxLength(50);
            e.HasIndex(a => a.Type);
            e.HasIndex(a => a.Status);
        });

        modelBuilder.Entity<ScanJob>(e =>
        {
            e.HasKey(j => j.Id);
            e.Property(j => j.Id).HasMaxLength(64);
            e.Property(j => j.AssetId).HasMaxLength(64);
            e.Property(j => j.ScanType).HasMaxLength(50);
            e.Property(j => j.Status).HasMaxLength(50);
            e.HasIndex(j => j.AssetId);
            e.HasIndex(j => j.Status);
        });

        modelBuilder.Entity<ScanResult>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasMaxLength(64);
            e.Property(r => r.JobId).HasMaxLength(64);
            e.Property(r => r.AssetId).HasMaxLength(64);
            e.Property(r => r.ScanType).HasMaxLength(50);
            e.HasIndex(r => r.JobId);
            e.HasIndex(r => r.AssetId);
            e.HasIndex(r => r.ScanType);
        });
    }
}
