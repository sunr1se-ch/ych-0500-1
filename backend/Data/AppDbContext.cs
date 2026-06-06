using Microsoft.EntityFrameworkCore;
using PlateTracking.Models;

namespace PlateTracking.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Plate> Plates => Set<Plate>();
    public DbSet<Impression> Impressions => Set<Impression>();
    public DbSet<AlignmentIncident> AlignmentIncidents => Set<AlignmentIncident>();
    public DbSet<Warning> Warnings => Set<Warning>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Plate>()
            .HasMany(p => p.Impressions)
            .WithOne(i => i.Plate)
            .HasForeignKey(i => i.PlateId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Plate>()
            .HasMany(p => p.Warnings)
            .WithOne(w => w.Plate)
            .HasForeignKey(w => w.PlateId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Plate>()
            .HasMany(p => p.AlignmentIncidents)
            .WithOne(a => a.Plate)
            .HasForeignKey(a => a.PlateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
