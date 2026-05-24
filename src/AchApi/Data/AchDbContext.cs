using AchApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace AchApi.Data;

public class AchDbContext(DbContextOptions<AchDbContext> options) : DbContext(options)
{
    public DbSet<AchFile> AchFiles => Set<AchFile>();
    public DbSet<AchEntry> AchEntries => Set<AchEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AchEntry>()
            .HasKey(e => e.EntryId);

        modelBuilder.Entity<AchFile>()
            .HasKey(f => f.FileId);

        modelBuilder.Entity<AchFile>()
            .HasMany(f => f.Entries)
            .WithOne()
            .HasForeignKey(e => e.FileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
